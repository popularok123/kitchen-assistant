package hub

import (
	"encoding/json"
	"log"
	"sync"
	"time"

	"github.com/google/uuid"
	"kitchen-assistant/server/internal/models"
)

type Hub struct {
	mu             sync.RWMutex
	clients        map[string]*Client        // connectionID -> Client
	messageHistory []*models.Message
	sessions       map[string]*models.CookingSession
	liveRooms      map[string]*models.LiveRoom
}

func NewHub() *Hub {
	return &Hub{
		clients:   make(map[string]*Client),
		sessions:  make(map[string]*models.CookingSession),
		liveRooms: make(map[string]*models.LiveRoom),
	}
}

// ── 连接管理 ──────────────────────────────────

func (h *Hub) Register(c *Client) {
	h.mu.Lock()
	h.clients[c.ID] = c
	h.mu.Unlock()
}

func (h *Hub) Unregister(c *Client) {
	h.mu.Lock()
	delete(h.clients, c.ID)
	h.mu.Unlock()

	// 通知其他人
	h.broadcast(models.WSMessage{Type: "user_left", Payload: c.User})
	h.broadcastOnlineUsers()

	// 清理直播间
	h.mu.Lock()
	for _, room := range h.liveRooms {
		if room.HostUserName == c.User.Name {
			room.IsLive = false
			delete(h.liveRooms, room.ID)
			h.mu.Unlock()
			h.broadcast(models.WSMessage{Type: "live_room_ended", Payload: room.ID})
			return
		}
		for i, v := range room.Viewers {
			if v.UserName == c.User.Name {
				room.Viewers = append(room.Viewers[:i], room.Viewers[i+1:]...)
				h.mu.Unlock()
				h.broadcastToRoom(room.ID, models.WSMessage{Type: "live_room_updated", Payload: room})
				return
			}
		}
	}
	h.mu.Unlock()
}

// ── 消息处理入口 ──────────────────────────────

func (h *Hub) HandleMessage(c *Client, raw []byte) {
	var msg models.WSMessage
	if err := json.Unmarshal(raw, &msg); err != nil {
		log.Printf("parse error: %v", err)
		return
	}

	payload, _ := json.Marshal(msg.Payload)

	switch msg.Type {
	case "join":
		h.handleJoin(c, payload)
	case "send_message":
		h.handleSendMessage(c, payload)
	case "send_private":
		h.handlePrivateMessage(c, payload)
	case "recall_message":
		h.handleRecallMessage(c, payload)
	// 协作烹饪
	case "create_session":
		h.handleCreateSession(c, payload)
	case "join_session":
		h.handleJoinSession(c, payload)
	case "assign_task":
		h.handleAssignTask(c, payload)
	case "complete_task":
		h.handleCompleteTask(c, payload)
	// 直播间
	case "create_live_room":
		h.handleCreateLiveRoom(c, payload)
	case "join_live_room":
		h.handleJoinLiveRoom(c, payload)
	case "leave_live_room":
		h.handleLeaveLiveRoom(c, payload)
	case "end_live_room":
		h.handleEndLiveRoom(c, payload)
	case "update_live_step":
		h.handleUpdateLiveStep(c, payload)
	case "send_danmaku":
		h.handleSendDanmaku(c, payload)
	case "ask_question":
		h.handleAskQuestion(c, payload)
	case "answer_question":
		h.handleAnswerQuestion(c, payload)
	// WebRTC 信令
	case "webrtc_request_stream":
		h.handleRequestStream(c, payload)
	case "webrtc_offer":
		h.handleWebRTCRelay(c, payload, "webrtc_offer")
	case "webrtc_answer":
		h.handleWebRTCRelay(c, payload, "webrtc_answer")
	case "webrtc_ice":
		h.handleWebRTCRelay(c, payload, "webrtc_ice")
	}
}

// ── Join ──────────────────────────────────────

func (h *Hub) handleJoin(c *Client, payload []byte) {
	var user models.User
	json.Unmarshal(payload, &user)
	user.ID = c.ID
	user.IsOnline = true
	user.JoinedAt = time.Now()
	c.User = user

	// 发送历史消息 + 在线用户 + 活跃会话 + 直播间
	h.mu.RLock()
	history := make([]*models.Message, len(h.messageHistory))
	copy(history, h.messageHistory)

	sessions := make([]*models.CookingSession, 0)
	for _, s := range h.sessions {
		sessions = append(sessions, s)
	}
	rooms := make([]*models.LiveRoom, 0)
	for _, r := range h.liveRooms {
		if r.IsLive {
			rooms = append(rooms, r)
		}
	}
	h.mu.RUnlock()

	c.Send(models.WSMessage{Type: "message_history", Payload: history})
	c.Send(models.WSMessage{Type: "active_sessions", Payload: sessions})
	c.Send(models.WSMessage{Type: "live_room_list", Payload: rooms})

	h.broadcast(models.WSMessage{Type: "user_joined", Payload: user})
	h.broadcastOnlineUsers()

	sysMsg := h.newSystemMessage(user.Name + " 加入了厨房")
	h.broadcast(models.WSMessage{Type: "new_message", Payload: sysMsg})
}

// ── 聊天消息 ──────────────────────────────────

func (h *Hub) handleSendMessage(c *Client, payload []byte) {
	var req struct{ Content string }
	json.Unmarshal(payload, &req)

	msg := &models.Message{
		ID:        uuid.NewString(),
		UserName:  c.User.Name,
		Avatar:    c.User.Avatar,
		Content:   req.Content,
		Type:      models.MsgNormal,
		Timestamp: time.Now(),
	}
	h.saveMessage(msg)
	h.broadcast(models.WSMessage{Type: "new_message", Payload: msg})
}

func (h *Hub) handlePrivateMessage(c *Client, payload []byte) {
	var req struct {
		TargetUserName string `json:"targetUserName"`
		Content        string `json:"content"`
	}
	json.Unmarshal(payload, &req)

	target := h.findClientByName(req.TargetUserName)
	if target == nil {
		c.Send(models.WSMessage{Type: "new_message", Payload: h.newSystemMessage("用户 " + req.TargetUserName + " 不在线")})
		return
	}

	msg := &models.Message{
		ID:             uuid.NewString(),
		UserName:       c.User.Name,
		Avatar:         c.User.Avatar,
		Content:        req.Content,
		Type:           models.MsgPrivate,
		TargetUserName: req.TargetUserName,
		Timestamp:      time.Now(),
	}
	target.Send(models.WSMessage{Type: "new_message", Payload: msg})
	c.Send(models.WSMessage{Type: "new_message", Payload: msg})
}

func (h *Hub) handleRecallMessage(c *Client, payload []byte) {
	var req struct{ MessageID string `json:"messageId"` }
	json.Unmarshal(payload, &req)

	h.mu.Lock()
	for i, m := range h.messageHistory {
		if m.ID == req.MessageID && m.UserName == c.User.Name {
			since := time.Since(m.Timestamp)
			if since.Minutes() <= 2 {
				h.messageHistory = append(h.messageHistory[:i], h.messageHistory[i+1:]...)
				h.mu.Unlock()
				h.broadcast(models.WSMessage{Type: "message_recalled", Payload: req.MessageID})
				return
			}
			break
		}
	}
	h.mu.Unlock()
}

// ── 协作烹饪 ──────────────────────────────────

func (h *Hub) handleCreateSession(c *Client, payload []byte) {
	var req struct {
		RecipeID   string `json:"recipeId"`
		RecipeName string `json:"recipeName"`
	}
	json.Unmarshal(payload, &req)

	session := &models.CookingSession{
		ID:           uuid.NewString(),
		Name:         c.User.Name + " 的 " + req.RecipeName,
		RecipeID:     req.RecipeID,
		RecipeName:   req.RecipeName,
		HostUserName: c.User.Name,
		Status:       models.SessionPreparing,
		StartTime:    time.Now(),
		Participants: []models.SessionParticipant{{
			UserName: c.User.Name,
			Avatar:   c.User.Avatar,
			JoinedAt: time.Now(),
			IsHost:   true,
		}},
	}

	h.mu.Lock()
	h.sessions[session.ID] = session
	h.mu.Unlock()

	h.broadcast(models.WSMessage{Type: "session_created", Payload: session})
	sysMsg := h.newSystemMessage("🎉 " + c.User.Name + " 创建了烹饪会话：" + session.Name)
	h.saveMessage(sysMsg)
	h.broadcast(models.WSMessage{Type: "new_message", Payload: sysMsg})
	c.Send(models.WSMessage{Type: "create_session_result", Payload: session.ID})
}

func (h *Hub) handleJoinSession(c *Client, payload []byte) {
	var req struct{ SessionID string `json:"sessionId"` }
	json.Unmarshal(payload, &req)

	h.mu.Lock()
	session, ok := h.sessions[req.SessionID]
	if ok {
		// 避免重复加入
		for _, p := range session.Participants {
			if p.UserName == c.User.Name {
				h.mu.Unlock()
				return
			}
		}
		session.Participants = append(session.Participants, models.SessionParticipant{
			UserName: c.User.Name,
			Avatar:   c.User.Avatar,
			JoinedAt: time.Now(),
		})
	}
	h.mu.Unlock()

	if ok {
		h.broadcast(models.WSMessage{Type: "session_updated", Payload: session})
		sysMsg := h.newSystemMessage("👋 " + c.User.Name + " 加入了「" + session.Name + "」")
		h.saveMessage(sysMsg)
		h.broadcast(models.WSMessage{Type: "new_message", Payload: sysMsg})
	}
}

func (h *Hub) handleAssignTask(c *Client, payload []byte) {
	var req struct {
		SessionID       string `json:"sessionId"`
		StepIndex       int    `json:"stepIndex"`
		StepDescription string `json:"stepDescription"`
	}
	json.Unmarshal(payload, &req)

	h.mu.Lock()
	session, ok := h.sessions[req.SessionID]
	if ok {
		now := time.Now()
		found := false
		for i, t := range session.TaskAssignments {
			if t.StepIndex == req.StepIndex {
				session.TaskAssignments[i].AssignedUserName = c.User.Name
				session.TaskAssignments[i].Status = models.TaskInProgress
				session.TaskAssignments[i].StartedAt = &now
				found = true
				break
			}
		}
		if !found {
			session.TaskAssignments = append(session.TaskAssignments, models.TaskAssignment{
				ID:               uuid.NewString(),
				StepIndex:        req.StepIndex,
				StepDescription:  req.StepDescription,
				AssignedUserName: c.User.Name,
				Status:           models.TaskInProgress,
				StartedAt:        &now,
			})
		}
	}
	h.mu.Unlock()
	if ok {
		h.broadcast(models.WSMessage{Type: "session_updated", Payload: session})
	}
}

func (h *Hub) handleCompleteTask(c *Client, payload []byte) {
	var req struct {
		SessionID string `json:"sessionId"`
		StepIndex int    `json:"stepIndex"`
	}
	json.Unmarshal(payload, &req)

	h.mu.Lock()
	session, ok := h.sessions[req.SessionID]
	if ok {
		now := time.Now()
		for i, t := range session.TaskAssignments {
			if t.StepIndex == req.StepIndex {
				session.TaskAssignments[i].Status = models.TaskCompleted
				session.TaskAssignments[i].CompletedAt = &now
				break
			}
		}
		allDone := true
		for _, t := range session.TaskAssignments {
			if t.Status != models.TaskCompleted {
				allDone = false
				break
			}
		}
		if allDone && len(session.TaskAssignments) > 0 {
			session.Status = models.SessionCompleted
		}
	}
	h.mu.Unlock()
	if ok {
		h.broadcast(models.WSMessage{Type: "session_updated", Payload: session})
	}
}

// ── 直播间 ────────────────────────────────────

func (h *Hub) handleCreateLiveRoom(c *Client, payload []byte) {
	var req struct {
		RecipeID   string `json:"recipeId"`
		RecipeName string `json:"recipeName"`
	}
	json.Unmarshal(payload, &req)

	room := &models.LiveRoom{
		ID:           uuid.NewString(),
		Name:         c.User.Name + " 的直播间",
		HostUserName: c.User.Name,
		RecipeID:     req.RecipeID,
		RecipeName:   req.RecipeName,
		IsLive:       true,
		StartTime:    time.Now(),
		Viewers:      []models.LiveViewer{},
		Questions:    []models.LiveQuestion{},
	}

	h.mu.Lock()
	h.liveRooms[room.ID] = room
	h.mu.Unlock()

	h.broadcast(models.WSMessage{Type: "live_room_created", Payload: room})
	c.Send(models.WSMessage{Type: "create_live_room_result", Payload: room.ID})
}

func (h *Hub) handleJoinLiveRoom(c *Client, payload []byte) {
	var req struct{ RoomID string `json:"roomId"` }
	json.Unmarshal(payload, &req)

	h.mu.Lock()
	room, ok := h.liveRooms[req.RoomID]
	if ok {
		exists := false
		for _, v := range room.Viewers {
			if v.UserName == c.User.Name {
				exists = true
				break
			}
		}
		if !exists {
			room.Viewers = append(room.Viewers, models.LiveViewer{
				UserName: c.User.Name,
				Avatar:   c.User.Avatar,
				JoinedAt: time.Now(),
			})
		}
	}
	h.mu.Unlock()

	if ok {
		c.CurrentRoomID = req.RoomID
		h.broadcastToRoom(req.RoomID, models.WSMessage{Type: "live_room_updated", Payload: room})
		// 通知主播有新观众请求推流
		host := h.findClientByName(room.HostUserName)
		if host != nil {
			host.Send(models.WSMessage{Type: "viewer_wants_stream", Payload: c.ID})
		}
	}
}

func (h *Hub) handleLeaveLiveRoom(c *Client, payload []byte) {
	var req struct{ RoomID string `json:"roomId"` }
	json.Unmarshal(payload, &req)

	h.mu.Lock()
	room, ok := h.liveRooms[req.RoomID]
	if ok {
		for i, v := range room.Viewers {
			if v.UserName == c.User.Name {
				room.Viewers = append(room.Viewers[:i], room.Viewers[i+1:]...)
				break
			}
		}
	}
	h.mu.Unlock()

	if ok {
		c.CurrentRoomID = ""
		h.broadcastToRoom(req.RoomID, models.WSMessage{Type: "live_room_updated", Payload: room})
	}
}

func (h *Hub) handleEndLiveRoom(c *Client, payload []byte) {
	var req struct{ RoomID string `json:"roomId"` }
	json.Unmarshal(payload, &req)

	h.mu.Lock()
	room, ok := h.liveRooms[req.RoomID]
	authorized := ok && room.HostUserName == c.User.Name
	if authorized {
		room.IsLive = false
		delete(h.liveRooms, req.RoomID)
	}
	h.mu.Unlock()

	if authorized {
		h.broadcast(models.WSMessage{Type: "live_room_ended", Payload: req.RoomID})
	}
}

func (h *Hub) handleUpdateLiveStep(c *Client, payload []byte) {
	var req struct {
		RoomID    string `json:"roomId"`
		StepIndex int    `json:"stepIndex"`
	}
	json.Unmarshal(payload, &req)

	h.mu.Lock()
	room, ok := h.liveRooms[req.RoomID]
	if ok && room.HostUserName == c.User.Name {
		room.CurrentStep = req.StepIndex
	}
	h.mu.Unlock()

	if ok {
		h.broadcastToRoom(req.RoomID, models.WSMessage{Type: "live_room_updated", Payload: room})
	}
}

func (h *Hub) handleSendDanmaku(c *Client, payload []byte) {
	var req struct {
		RoomID  string `json:"roomId"`
		Content string `json:"content"`
	}
	json.Unmarshal(payload, &req)

	danmaku := models.Danmaku{
		RoomID:    req.RoomID,
		UserName:  c.User.Name,
		Avatar:    c.User.Avatar,
		Content:   req.Content,
		Timestamp: time.Now(),
	}
	h.broadcastToRoom(req.RoomID, models.WSMessage{Type: "danmaku", Payload: danmaku})
}

func (h *Hub) handleAskQuestion(c *Client, payload []byte) {
	var req struct {
		RoomID  string `json:"roomId"`
		Content string `json:"content"`
	}
	json.Unmarshal(payload, &req)

	h.mu.Lock()
	room, ok := h.liveRooms[req.RoomID]
	if ok {
		q := models.LiveQuestion{
			ID:          uuid.NewString(),
			AskUserName: c.User.Name,
			Content:     req.Content,
			AskedAt:     time.Now(),
		}
		room.Questions = append(room.Questions, q)
		h.mu.Unlock()
		h.broadcastToRoom(req.RoomID, models.WSMessage{Type: "question_asked", Payload: q})
	} else {
		h.mu.Unlock()
	}
}

func (h *Hub) handleAnswerQuestion(c *Client, payload []byte) {
	var req struct {
		RoomID     string `json:"roomId"`
		QuestionID string `json:"questionId"`
		Answer     string `json:"answer"`
	}
	json.Unmarshal(payload, &req)

	h.mu.Lock()
	room, ok := h.liveRooms[req.RoomID]
	var answered *models.LiveQuestion
	if ok && room.HostUserName == c.User.Name {
		for i := range room.Questions {
			if room.Questions[i].ID == req.QuestionID {
				room.Questions[i].Answer = req.Answer
				room.Questions[i].IsAnswered = true
				q := room.Questions[i]
				answered = &q
				break
			}
		}
	}
	h.mu.Unlock()

	if answered != nil {
		h.broadcastToRoom(req.RoomID, models.WSMessage{Type: "question_answered", Payload: answered})
	}
}

// ── WebRTC 信令中继 ───────────────────────────

func (h *Hub) handleRequestStream(c *Client, payload []byte) {
	var req struct{ RoomID string `json:"roomId"` }
	json.Unmarshal(payload, &req)

	h.mu.RLock()
	room, ok := h.liveRooms[req.RoomID]
	h.mu.RUnlock()
	if !ok {
		return
	}

	host := h.findClientByName(room.HostUserName)
	if host != nil {
		host.Send(models.WSMessage{Type: "viewer_wants_stream", Payload: c.ID})
	}
}

func (h *Hub) handleWebRTCRelay(c *Client, payload []byte, msgType string) {
	var req struct {
		TargetID string `json:"targetId"`
		SDP      string `json:"sdp,omitempty"`
		Candidate string `json:"candidate,omitempty"`
	}
	json.Unmarshal(payload, &req)

	h.mu.RLock()
	target, ok := h.clients[req.TargetID]
	h.mu.RUnlock()

	if ok {
		target.Send(models.WSMessage{
			Type: msgType,
			Payload: map[string]string{
				"fromId":    c.ID,
				"sdp":       req.SDP,
				"candidate": req.Candidate,
			},
		})
	}
}

// ── 工具方法 ──────────────────────────────────

func (h *Hub) broadcast(msg models.WSMessage) {
	h.mu.RLock()
	defer h.mu.RUnlock()
	for _, c := range h.clients {
		c.Send(msg)
	}
}

func (h *Hub) broadcastToRoom(roomID string, msg models.WSMessage) {
	h.mu.RLock()
	room, ok := h.liveRooms[roomID]
	h.mu.RUnlock()
	if !ok {
		return
	}

	h.mu.RLock()
	defer h.mu.RUnlock()
	for _, c := range h.clients {
		if c.User.Name == room.HostUserName {
			c.Send(msg)
			continue
		}
		for _, v := range room.Viewers {
			if v.UserName == c.User.Name {
				c.Send(msg)
				break
			}
		}
	}
}

func (h *Hub) broadcastOnlineUsers() {
	h.mu.RLock()
	users := make([]models.User, 0, len(h.clients))
	for _, c := range h.clients {
		users = append(users, c.User)
	}
	h.mu.RUnlock()
	h.broadcast(models.WSMessage{Type: "online_users", Payload: users})
}

func (h *Hub) findClientByName(name string) *Client {
	h.mu.RLock()
	defer h.mu.RUnlock()
	for _, c := range h.clients {
		if c.User.Name == name {
			return c
		}
	}
	return nil
}

func (h *Hub) saveMessage(msg *models.Message) {
	h.mu.Lock()
	h.messageHistory = append(h.messageHistory, msg)
	if len(h.messageHistory) > 100 {
		h.messageHistory = h.messageHistory[1:]
	}
	h.mu.Unlock()
}

func (h *Hub) newSystemMessage(content string) *models.Message {
	msg := &models.Message{
		ID:        uuid.NewString(),
		UserName:  "系统",
		Content:   content,
		Type:      models.MsgSystem,
		Timestamp: time.Now(),
	}
	h.saveMessage(msg)
	return msg
}

func (h *Hub) GetSessions() []*models.CookingSession {
	h.mu.RLock()
	defer h.mu.RUnlock()
	result := make([]*models.CookingSession, 0, len(h.sessions))
	for _, s := range h.sessions {
		result = append(result, s)
	}
	return result
}
