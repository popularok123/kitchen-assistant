package models

import "time"

// ── 用户 ──────────────────────────────────────
type User struct {
	ID        string    `json:"id"`
	Name      string    `json:"name"`
	Avatar    string    `json:"avatar"`
	JoinedAt  time.Time `json:"joinedAt"`
	IsOnline  bool      `json:"isOnline"`
}

// ── 消息 ──────────────────────────────────────
type MessageType string

const (
	MsgNormal   MessageType = "normal"
	MsgSystem   MessageType = "system"
	MsgPrivate  MessageType = "private"
	MsgProgress MessageType = "progress"
)

type Message struct {
	ID             string      `json:"id"`
	UserName       string      `json:"userName"`
	Avatar         string      `json:"avatar"`
	Content        string      `json:"content"`
	Type           MessageType `json:"type"`
	TargetUserName string      `json:"targetUserName,omitempty"`
	Timestamp      time.Time   `json:"timestamp"`
}

// ── 菜谱 ──────────────────────────────────────
type Recipe struct {
	ID              string        `json:"id"`
	Name            string        `json:"name"`
	Description     string        `json:"description"`
	CookTimeMinutes int           `json:"cookTimeMinutes"`
	DifficultyLevel int           `json:"difficultyLevel"`
	Ingredients     []string      `json:"ingredients"`
	Steps           []CookingStep `json:"steps"`
	Tips            []string      `json:"tips"`
	IsCustom        bool          `json:"isCustom"`
}

type CookingStep struct {
	Order           int    `json:"order"`
	Instruction     string `json:"instruction"`
	DurationSeconds int    `json:"durationSeconds"`
}

// ── 购物清单 ──────────────────────────────────
type ShoppingItem struct {
	ID          string     `json:"id"`
	Name        string     `json:"name"`
	Quantity    string     `json:"quantity"`
	Category    string     `json:"category"`
	IsPurchased bool       `json:"isPurchased"`
	AddedAt     time.Time  `json:"addedAt"`
	PurchasedAt *time.Time `json:"purchasedAt,omitempty"`
}

// ── 协作烹饪 ──────────────────────────────────
type TaskStatus string

const (
	TaskPending    TaskStatus = "pending"
	TaskInProgress TaskStatus = "inProgress"
	TaskCompleted  TaskStatus = "completed"
)

type TaskAssignment struct {
	ID               string     `json:"id"`
	StepIndex        int        `json:"stepIndex"`
	StepDescription  string     `json:"stepDescription"`
	AssignedUserName string     `json:"assignedUserName"`
	Status           TaskStatus `json:"status"`
	StartedAt        *time.Time `json:"startedAt,omitempty"`
	CompletedAt      *time.Time `json:"completedAt,omitempty"`
}

type SessionStatus string

const (
	SessionPreparing SessionStatus = "preparing"
	SessionCooking   SessionStatus = "cooking"
	SessionCompleted SessionStatus = "completed"
)

type SessionParticipant struct {
	UserName string    `json:"userName"`
	Avatar   string    `json:"avatar"`
	JoinedAt time.Time `json:"joinedAt"`
	IsHost   bool      `json:"isHost"`
}

type CookingSession struct {
	ID              string               `json:"id"`
	Name            string               `json:"name"`
	RecipeID        string               `json:"recipeId"`
	RecipeName      string               `json:"recipeName"`
	HostUserName    string               `json:"hostUserName"`
	Participants    []SessionParticipant `json:"participants"`
	TaskAssignments []TaskAssignment     `json:"taskAssignments"`
	CurrentStep     int                  `json:"currentStep"`
	Status          SessionStatus        `json:"status"`
	StartTime       time.Time            `json:"startTime"`
}

// ── 直播间 ────────────────────────────────────
type LiveQuestion struct {
	ID           string     `json:"id"`
	AskUserName  string     `json:"askUserName"`
	Content      string     `json:"content"`
	Answer       string     `json:"answer,omitempty"`
	IsAnswered   bool       `json:"isAnswered"`
	AskedAt      time.Time  `json:"askedAt"`
}

type Danmaku struct {
	RoomID    string    `json:"roomId"`
	UserName  string    `json:"userName"`
	Avatar    string    `json:"avatar"`
	Content   string    `json:"content"`
	Timestamp time.Time `json:"timestamp"`
}

type LiveViewer struct {
	UserName string    `json:"userName"`
	Avatar   string    `json:"avatar"`
	JoinedAt time.Time `json:"joinedAt"`
}

type LiveRoom struct {
	ID           string         `json:"id"`
	Name         string         `json:"name"`
	HostUserName string         `json:"hostUserName"`
	RecipeID     string         `json:"recipeId"`
	RecipeName   string         `json:"recipeName"`
	CurrentStep  int            `json:"currentStep"`
	Viewers      []LiveViewer   `json:"viewers"`
	Questions    []LiveQuestion `json:"questions"`
	IsLive       bool           `json:"isLive"`
	StartTime    time.Time      `json:"startTime"`
}

// ── WebSocket 消息包 ──────────────────────────
type WSMessage struct {
	Type    string `json:"type"`
	Payload any    `json:"payload"`
}
