package hub

import (
	"encoding/json"
	"log"
	"time"

	"github.com/gorilla/websocket"
	"kitchen-assistant/server/internal/models"
)

const (
	writeWait  = 10 * time.Second
	pongWait   = 60 * time.Second
	pingPeriod = 50 * time.Second
	maxMsgSize = 512 * 1024
)

type Client struct {
	ID            string
	User          models.User
	CurrentRoomID string
	hub           *Hub
	conn          *websocket.Conn
	send          chan models.WSMessage
}

func NewClient(id string, hub *Hub, conn *websocket.Conn) *Client {
	return &Client{
		ID:   id,
		hub:  hub,
		conn: conn,
		send: make(chan models.WSMessage, 256),
	}
}

func (c *Client) Send(msg models.WSMessage) {
	select {
	case c.send <- msg:
	default:
		log.Printf("client %s send buffer full", c.ID)
	}
}

func (c *Client) ReadPump() {
	defer func() {
		c.hub.Unregister(c)
		c.conn.Close()
	}()

	c.conn.SetReadLimit(maxMsgSize)
	c.conn.SetReadDeadline(time.Now().Add(pongWait))
	c.conn.SetPongHandler(func(string) error {
		c.conn.SetReadDeadline(time.Now().Add(pongWait))
		return nil
	})

	for {
		_, raw, err := c.conn.ReadMessage()
		if err != nil {
			break
		}
		c.hub.HandleMessage(c, raw)
	}
}

func (c *Client) WritePump() {
	ticker := time.NewTicker(pingPeriod)
	defer func() {
		ticker.Stop()
		c.conn.Close()
	}()

	for {
		select {
		case msg, ok := <-c.send:
			c.conn.SetWriteDeadline(time.Now().Add(writeWait))
			if !ok {
				c.conn.WriteMessage(websocket.CloseMessage, []byte{})
				return
			}
			data, err := json.Marshal(msg)
			if err != nil {
				continue
			}
			if err := c.conn.WriteMessage(websocket.TextMessage, data); err != nil {
				return
			}
		case <-ticker.C:
			c.conn.SetWriteDeadline(time.Now().Add(writeWait))
			if err := c.conn.WriteMessage(websocket.PingMessage, nil); err != nil {
				return
			}
		}
	}
}
