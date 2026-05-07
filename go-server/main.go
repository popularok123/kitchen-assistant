package main

import (
	"log"

	"github.com/gin-gonic/gin"
	"kitchen-assistant/server/internal/data"
	"kitchen-assistant/server/internal/handlers"
	"kitchen-assistant/server/internal/hub"
)

func main() {
	h := hub.NewHub()
	store := handlers.NewRecipeStore(data.SeedRecipes())

	r := gin.Default()

	// CORS
	r.Use(func(c *gin.Context) {
		c.Header("Access-Control-Allow-Origin", "*")
		c.Header("Access-Control-Allow-Methods", "GET,POST,PUT,DELETE,OPTIONS")
		c.Header("Access-Control-Allow-Headers", "Content-Type,Authorization")
		if c.Request.Method == "OPTIONS" {
			c.AbortWithStatus(204)
			return
		}
		c.Next()
	})

	// ── REST API ──
	api := r.Group("/api")
	{
		api.GET("/recipes", store.List())
		api.GET("/recipes/:id", store.Get())
		api.POST("/recipes", store.Create())
		api.DELETE("/recipes/:id", store.Delete())
	}

	// ── WebSocket ──
	r.GET("/ws", handlers.WSHandler(h))

	// ── 健康检查 ──
	r.GET("/", func(c *gin.Context) {
		c.JSON(200, gin.H{"status": "🍳 Kitchen Assistant Go Server"})
	})

	log.Println("🚀 Server running on :8080")
	if err := r.Run(":8080"); err != nil {
		log.Fatal(err)
	}
}
