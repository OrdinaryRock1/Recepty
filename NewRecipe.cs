namespace Recepty;

public class NewRecipe
{
    public string Title { get; set; }
    public string Description { get; set; }
    public int PrepTime { get; set; }
    public int CookTime { get; set; }
    public string Diet { get; set; }
    public string Cuisine { get; set; }
    public List<NewIngredient> Ingredients { get; set; }
    public List<NewStep> Steps { get; set; }
}