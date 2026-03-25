using Microsoft.Extensions.Configuration;

namespace Recepty;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Dapper;
[ApiController]
[Route("api/[controller]")]
public class RecipesController : ControllerBase
{
    private readonly IConfiguration _config;

    public RecipesController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchRecipes([FromQuery] string? query, [FromQuery] string? diet)
    {
        string connectionString = _config.GetConnectionString("RecipeDatabase");
        using (var connection = new MySqlConnection(connectionString))
        {
            string sql = @"
                    SELECT recipe_id, title, prep_time_minutes as Prep, cook_time_minutes as Cook, dietary_label as Diet
                    FROM Recipes 
                    WHERE (@Diet = 'all' OR dietary_label = @Diet)
                    AND (@Query IS NULL OR title LIKE CONCAT('%', @Query, '%'));";
            var recipes = await connection.QueryAsync(sql, new { Diet = diet, Query = query });
            return Ok(recipes); 
        }
    }
    
    [HttpPost("add")]
public async Task<IActionResult> AddRecipe([FromBody] NewRecipe recipe)
{
    string connectionString = _config.GetConnectionString("RecipeDatabase");

    using (var connection = new MySqlConnection(connectionString))
    {
        // 1. Insert Recipe and grab the newly generated recipe_id (LAST_INSERT_ID)
        string recipeSql = @"
            INSERT INTO Recipes (title, description, prep_time_minutes, cook_time_minutes, dietary_label, cuisine_type)
            VALUES (@Title, @Description, @PrepTime, @CookTime, @Diet, @Cuisine);
            SELECT LAST_INSERT_ID();";
            
        // ExecuteScalarAsync returns the specific ID that MySQL just created
        int newRecipeId = await connection.ExecuteScalarAsync<int>(recipeSql, recipe);

        // 2. Loop through the ingredients the user sent
        if (recipe.Ingredients != null)
        {
            foreach (var ing in recipe.Ingredients)
            {
                // A. Insert ingredient name into dictionary (IGNORE it if it already exists)
                string ingSql = "INSERT IGNORE INTO Ingredients (name) VALUES (@Name);";
                await connection.ExecuteAsync(ingSql, new { Name = ing.Name });

                // B. Fetch the ingredient_id (whether it was just created or already existed)
                int ingId = await connection.QuerySingleAsync<int>(
                    "SELECT ingredient_id FROM Ingredients WHERE name = @Name;", new { Name = ing.Name });

                // C. Map the Recipe to the Ingredient with the quantity and unit
                string mapSql = @"
                    INSERT INTO RecipeIngredients (recipe_id, ingredient_id, quantity, unit) 
                    VALUES (@RecipeId, @IngId, @Qty, @Unit);";
                await connection.ExecuteAsync(mapSql, new { 
                    RecipeId = newRecipeId, 
                    IngId = ingId, 
                    Qty = ing.Quantity, 
                    Unit = ing.Unit 
                });
            }
            // 3. Save the Cooking Steps
            if (recipe.Steps != null)
            {
                foreach (var step in recipe.Steps)
                {
                    string stepSql = @"
                    INSERT INTO Steps (recipe_id, step_number, instruction) 
                    VALUES (@RecipeId, @StepNum, @Instruction);";
                
                    await connection.ExecuteAsync(stepSql, new { 
                        RecipeId = newRecipeId, 
                        StepNum = step.StepNumber, 
                        Instruction = step.Instruction 
                    });
                }
            }
        }

        return Ok(new { message = "Recipe and Ingredients added successfully!" }); 
    }
}
    
    // The "{id}" means it expects a number at the end of the URL
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRecipeDetails(int id)
    {
        string connectionString = _config.GetConnectionString("RecipeDatabase");

        using (var connection = new MySqlConnection(connectionString))
        {
            // 1. Get the core recipe info
            var recipe = await connection.QueryFirstOrDefaultAsync(
                "SELECT * FROM Recipes WHERE recipe_id = @Id", new { Id = id });

            if (recipe == null) return NotFound(new { message = "Recipe not found." });

            // 2. Get the Ingredients using a JOIN
            string ingredientsSql = @"
            SELECT i.name, ri.quantity, ri.unit 
            FROM RecipeIngredients ri
            JOIN Ingredients i ON ri.ingredient_id = i.ingredient_id
            WHERE ri.recipe_id = @Id;";
            var ingredients = await connection.QueryAsync(ingredientsSql, new { Id = id });

            // 3. Get the Cooking Steps
            string stepsSql = @"
            SELECT step_number, instruction 
            FROM Steps 
            WHERE recipe_id = @Id 
            ORDER BY step_number ASC;";
            var steps = await connection.QueryAsync(stepsSql, new { Id = id });

            // 4. Package it all up into one JSON object and send it back!
            return Ok(new { 
                Details = recipe, 
                Ingredients = ingredients, 
                Steps = steps 
            });
        }
    }
}
