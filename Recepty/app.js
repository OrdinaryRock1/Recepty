async function searchRecipes() {
    const searchQuery = document.getElementById('searchInput').value;
    const dietFilter = document.getElementById('dietFilter').value;
    const container = document.getElementById('recipeContainer');

    container.innerHTML = '<p class="status-msg">Načítám skvělé recepty...</p>';

    try {
        const response = await fetch(`http://localhost:5000/api/recipes/search?query=${searchQuery}&diet=${dietFilter}`);

        if (!response.ok) {
            container.innerHTML = '<p class="error-msg">Něco se pokazilo. Zkuste to prosím znovu.</p>';
            return;
        }

        const recipesData = await response.json();
        container.innerHTML = '';

        if (recipesData.length === 0) {
            container.innerHTML = '<p class="status-msg">Nebyly nalezeny žádné recepty. Zkuste jiné vyhledávání!</p>';
            return;
        }
        

        recipesData.forEach(recipe => {
            const card = document.createElement('div');
            card.className = 'recipe-card';

            // Zjištění, zda má recept obrázek z databáze
            const imgData = recipe.imageData || recipe.ImageData;
            const bgStyle = imgData
                ? `background-image: url('${imgData}'); background-size: cover; background-position: center;`
                : `background: linear-gradient(45deg, #ff9a9e 0%, #fecfef 99%, #fecfef 100%);`;

            card.innerHTML = `
                <div class="card-image" style="${bgStyle}"></div>
                <div class="card-content">
                    <h3>${recipe.title || recipe.Title}</h3>
                    <p>Příprava: <strong>${recipe.prep || recipe.Prep} min</strong> | Vaření: <strong>${recipe.cook || recipe.Cook} min</strong></p>
                    <div class="tags">
                        <span class="tag diet">${recipe.diet || recipe.Diet || 'Standardní'}</span>
                    </div>
                    <button class="btn btn-outline card-btn" onclick="viewRecipe(${recipe.recipe_id || recipe.Recipe_id})">Zobrazit detaily</button>
                </div>
            `;
            container.appendChild(card);
        });

    } catch (error) {
        container.innerHTML = '<p class="error-msg">Chyba připojení k databázi. Běží váš C# backend?</p>';
        console.error(error);
    }
}

async function viewRecipe(id) {
    try {
        const response = await fetch(`http://localhost:5000/api/recipes/${id}`);
        if (!response.ok) {
            alert("Nepodařilo se načíst detaily receptu.");
            return;
        }

        const data = await response.json();

        document.getElementById('detailTitle').innerText = data.details.title || data.details.Title;
        document.getElementById('detailDesc').innerText = data.details.description || data.details.Description;

        const ingredientsList = document.getElementById('detailIngredients');
        ingredientsList.innerHTML = '';

        if (data.ingredients.length === 0) {
            ingredientsList.innerHTML = '<li>Zatím žádné ingredience.</li>';
        } else {
            data.ingredients.forEach(ing => {
                // Handle different JSON casing from C#
                const qty = ing.quantity || ing.Quantity;
                const unit = ing.unit || ing.Unit || '';
                const name = ing.name || ing.Name;
                ingredientsList.innerHTML += `<li><strong>${qty} ${unit}</strong> ${name}</li>`;
            });
        }

        const stepsList = document.getElementById('detailSteps');
        stepsList.innerHTML = '';

        if (data.steps.length === 0) {
            stepsList.innerHTML = '<li>Zatím žádný postup.</li>';
        } else {
            data.steps.forEach(step => {
                const instruction = step.instruction || step.Instruction;
                stepsList.innerHTML += `<li style="margin-bottom: 10px;">${instruction}</li>`;
            });
        }

        document.getElementById('detailsModal').style.display = 'block';
    } catch (error) {
        console.error('Chyba:', error);
    }
}

function openModal() { document.getElementById('recipeModal').style.display = 'block'; }
function closeModal() { document.getElementById('recipeModal').style.display = 'none'; }
function closeDetailsModal() { document.getElementById('detailsModal').style.display = 'none'; }

function addIngredientRow() {
    const row = document.createElement('div');
    row.className = 'ingredient-row';
    row.innerHTML = `
        <input type="text" class="ing-name" placeholder="Název" required>
        <input type="number" step="0.1" class="ing-qty" placeholder="Množství" required>
        <input type="text" class="ing-unit" placeholder="Jednotka">
        <button type="button" class="btn btn-danger" onclick="this.parentElement.remove()">X</button>
    `;
    document.getElementById('ingredientList').appendChild(row);
}

function addStepRow() {
    const list = document.getElementById('stepList');
    const stepCount = list.children.length + 1;

    const row = document.createElement('div');
    row.className = 'step-row';
    row.innerHTML = `
        <span class="step-number">${stepCount}.</span>
        <textarea class="step-desc" placeholder="Popište tento krok..." required></textarea>
        <button type="button" class="btn btn-danger" onclick="this.parentElement.remove(); updateStepNumbers();">X</button>
    `;
    list.appendChild(row);
}

function updateStepNumbers() {
    const rows = document.querySelectorAll('.step-row .step-number');
    rows.forEach((span, index) => {
        span.innerText = `${index + 1}.`;
    });
}

async function submitRecipe(event) {
    event.preventDefault();

    const ingredientRows = document.querySelectorAll('.ingredient-row');
    const ingredientsArray = [];
    ingredientRows.forEach(row => {
        ingredientsArray.push({
            name: row.querySelector('.ing-name').value,
            quantity: parseFloat(row.querySelector('.ing-qty').value),
            unit: row.querySelector('.ing-unit').value || ""
        });
    });

    const stepRows = document.querySelectorAll('.step-row');
    const stepsArray = [];
    stepRows.forEach((row, index) => {
        stepsArray.push({
            stepNumber: index + 1,
            instruction: row.querySelector('.step-desc').value
        });
    });
    
    const newRecipe = {
        title: document.getElementById('newTitle').value,
        description: document.getElementById('newDesc').value,
        prepTime: parseInt(document.getElementById('newPrep').value),
        cookTime: parseInt(document.getElementById('newCook').value),
        diet: document.getElementById('newDiet').value,
        cuisine: 'Obecné',
        ingredients: ingredientsArray,
        steps: stepsArray
    };

    try {
        const response = await fetch('http://localhost:5000/api/recipes/add', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(newRecipe)
        });

        if (response.ok) {
            alert('Recept byl úspěšně přidán!');
            closeModal();
            document.getElementById('addRecipeForm').reset();
            searchRecipes();
        } else {
            alert('Chyba při přidávání receptu.');
        }
    } catch (error) {
        console.error('Chyba odeslání:', error);
    }
}


window.onload = searchRecipes;