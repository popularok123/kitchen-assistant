namespace KitchenAssistant.Core.Models;

public class Recipe
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CookTimeMinutes { get; set; }
    public int DifficultyLevel { get; set; }
    public List<string> Ingredients { get; set; } = [];
    public List<CookingStep> Steps { get; set; } = [];
    public List<string> Tips { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public class CookingStep
{
    public int Order { get; set; }
    public string Instruction { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public bool IsOptional { get; set; }
}
