
using ToDoList.Core.Repositories;
using ToDoList.Data;
using ToDoList.Models;

namespace ToDoList.Tests
{
    public class CategoryRepositoryTests : IAsyncLifetime
    {
        private readonly string _databasePath;
        private readonly AppDatabase _database;
        private readonly CategoryRepository _reposForCategories;

        public CategoryRepositoryTests()
        {
            _databasePath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}.db3"
            );

            _database = new AppDatabase(_databasePath);
            _reposForCategories = new CategoryRepository(_database);
        }

        public async Task InitializeAsync()
        {
            await _database.GetConnectionAsync();
        }

        public async Task DisposeAsync()
        {
            await _database.CloseAsync();

            if (File.Exists(_databasePath))
                File.Delete(_databasePath);
        }



        [Fact]
        public async Task GetAllAsync_ShouldReturnFiveCategories()
        {
            List<Category> categories =
                await _reposForCategories.GetAllAsync();

            Assert.NotNull(categories);
            Assert.Equal(5, categories.Count);

            Assert.Equal(5,
                categories.Select(c => c.Id).Distinct().Count());
        }



        [Fact]
        public async Task GetAllAsync_ShouldReturnCategoriesWithCorrectData()
        {
            List<Category> categories =
                await _reposForCategories.GetAllAsync();

            Assert.Equal(5, categories.Count);


            Assert.Equal(
                new[] { 1, 2, 3, 4, 5 },
                categories.Select(c => c.Id).ToArray()
            );

            Assert.Equal("Работа", categories[0].Name);
            Assert.Equal("Учеба", categories[1].Name);
            Assert.Equal("Дом", categories[2].Name);
            Assert.Equal("Покупки", categories[3].Name);
            Assert.Equal("Личное", categories[4].Name);


            Assert.Equal("#EF4444", categories[0].Color);
            Assert.Equal("#A78BFA", categories[1].Color);
            Assert.Equal("#86EFAC", categories[2].Color);
            Assert.Equal("#FACC15", categories[3].Color);
            Assert.Equal("#60A5FA", categories[4].Color);
        }

    }
}
