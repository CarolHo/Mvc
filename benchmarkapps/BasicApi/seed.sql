INSERT INTO main.Categories (Name)
VALUES ('Dogs'), ('Cats'), ('Rabbits'), ('Lions');

INSERT INTO main.Pets (Age, CategoryId, HasVaccinations, Name, Status)
SELECT 1, Id, 1, Name || '1', 'available' FROM main.Categories;

INSERT INTO main.Pets (Age, CategoryId, HasVaccinations, Name, Status)
SELECT 1, Id, 1, Name || '2', 'available' FROM main.Categories;

INSERT INTO main.Pets (Age, CategoryId, HasVaccinations, Name, Status)
SELECT 1, Id, 1, Name || '3', 'available' FROM main.Categories;

INSERT INTO main.Tags (Name, PetId)
SELECT 'Tag1', ID FROM main.Pets;

INSERT INTO main.Images ([Url], PetId)
SELECT 'http://example.com/pets/' || CAST(Id as varchar) || '_1.png', ID from main.Pets;