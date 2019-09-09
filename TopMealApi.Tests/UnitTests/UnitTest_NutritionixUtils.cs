using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;
using TopMealApi.Utils;

namespace TopMealApi.Tests
{
    public class UnitTest_NutritionixUtils
    {
        private readonly IConfiguration _configuration;
        private readonly bool _useNutritionixInTests;

        public UnitTest_NutritionixUtils()
        {
            _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            _useNutritionixInTests = _configuration.GetValue<string>("Nutritionix:UseInUnitTests") == "true";
        }

        private async Task TestNutritionixGetCalories(string description, int expectedRes)
        {
            var res = expectedRes;
            
            if (_useNutritionixInTests)
            {
                res = await NutritionixUtils.GetCaloriesIntAsync("62031f66", "aa0049adb09f7397e00c630db7952cf6", description);
            }
            Assert.Equal(res, expectedRes);
        }

        private void Ok()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async void Test1() => await TestNutritionixGetCalories("apple", 95);

        [Fact]
        public async void Test2() => await TestNutritionixGetCalories("apple bread", 274);

        [Fact]
        public async void Test3() => await TestNutritionixGetCalories("1 apple 1 bread", 172);

        [Fact]
        public async void Test4() => await TestNutritionixGetCalories("3 apple 2 bread", 438);

        [Fact]
        public async void Test5() => await TestNutritionixGetCalories("1kg apple", 520);

        [Fact]
        public async void Test6() => await TestNutritionixGetCalories("0.5kg apple", 260);

        [Fact]
        public async void Test7() => await TestNutritionixGetCalories("100g bread", 266);

        [Fact]
        public async void Test8() => await TestNutritionixGetCalories("0.5kg apple 100g bread", 526);

        [Fact]
        public async void Test9() => await TestNutritionixGetCalories("1 carrot", 16);

        [Fact]
        public async void Test10() => await TestNutritionixGetCalories("1 apple 1 carrot 1 bread", 188);
    }
}
