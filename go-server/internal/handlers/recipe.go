package handlers

import (
	"net/http"
	"strings"

	"github.com/gin-gonic/gin"
	"github.com/google/uuid"
	"kitchen-assistant/server/internal/models"
)

type RecipeStore struct {
	recipes []models.Recipe
}

func NewRecipeStore(seed []models.Recipe) *RecipeStore {
	return &RecipeStore{recipes: seed}
}

func (s *RecipeStore) List() gin.HandlerFunc {
	return func(c *gin.Context) {
		q := strings.ToLower(c.Query("q"))
		difficulty := c.Query("difficulty")
		result := s.recipes

		if q != "" {
			filtered := result[:0]
			for _, r := range result {
				if strings.Contains(strings.ToLower(r.Name), q) ||
					strings.Contains(strings.ToLower(r.Description), q) {
					filtered = append(filtered, r)
				}
			}
			result = filtered
		}
		if difficulty != "" {
			filtered := result[:0]
			for _, r := range result {
				d := ""
				switch r.DifficultyLevel {
				case 1:
					d = "1"
				case 2:
					d = "2"
				case 3:
					d = "3"
				}
				if d == difficulty {
					filtered = append(filtered, r)
				}
			}
			result = filtered
		}
		c.JSON(http.StatusOK, result)
	}
}

func (s *RecipeStore) Get() gin.HandlerFunc {
	return func(c *gin.Context) {
		id := c.Param("id")
		for _, r := range s.recipes {
			if r.ID == id {
				c.JSON(http.StatusOK, r)
				return
			}
		}
		c.JSON(http.StatusNotFound, gin.H{"error": "not found"})
	}
}

func (s *RecipeStore) Create() gin.HandlerFunc {
	return func(c *gin.Context) {
		var recipe models.Recipe
		if err := c.ShouldBindJSON(&recipe); err != nil {
			c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
			return
		}
		recipe.ID = uuid.NewString()
		recipe.IsCustom = true
		s.recipes = append(s.recipes, recipe)
		c.JSON(http.StatusCreated, recipe)
	}
}

func (s *RecipeStore) Delete() gin.HandlerFunc {
	return func(c *gin.Context) {
		id := c.Param("id")
		for i, r := range s.recipes {
			if r.ID == id && r.IsCustom {
				s.recipes = append(s.recipes[:i], s.recipes[i+1:]...)
				c.Status(http.StatusNoContent)
				return
			}
		}
		c.JSON(http.StatusNotFound, gin.H{"error": "not found or not custom"})
	}
}
