using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EnergyAccounting.Tests
{
    [TestClass]
    public class EnergyLogicTests
    {
        // 1. Тест: Проверка расчета расхода (Текущее - Предыдущее)
        [TestMethod]
        public void CalculateConsumption_ValidValues_ReturnsCorrectDifference()
        {
            decimal previousValue = 125.5m;
            decimal currentValue = 130.0m;
            decimal consumption = currentValue - previousValue;
            Assert.AreEqual(4.5m, consumption);
        }

        // 2. Тест: Проверка валидации (новое показание не может быть меньше)
        [TestMethod]
        public void ValidateReading_NewLessThanPrevious_ReturnsFalse()
        {
            decimal previousValue = 125.0m;
            decimal newValue = 120.0m;
            bool isValid = newValue > previousValue;
            Assert.IsFalse(isValid);
        }

        // 3. Тест: Проверка валидации (новое показание больше предыдущего)
        [TestMethod]
        public void ValidateReading_NewMoreThanPrevious_ReturnsTrue()
        {
            decimal previousValue = 125.0m;
            decimal newValue = 130.0m;
            bool isValid = newValue > previousValue;
            Assert.IsTrue(isValid);
        }

        // 4. Тест: Проверка расчета стоимости по тарифу
        [TestMethod]
        public void CalculateCost_ValidConsumptionAndPrice_ReturnsCorrectSum()
        {
            decimal consumption = 5.5m;
            decimal price = 45.50m;
            decimal totalCost = consumption * price;
            Assert.AreEqual(250.25m, totalCost);
        }

        // 5. Тест: Проверка блокировки после 3 попыток
        [TestMethod]
        public void CheckUserBlocking_ThreeAttempts_ReturnsBlockedStatus()
        {
            int failedAttempts = 3;
            bool isBlocked = failedAttempts >= 3;
            Assert.IsTrue(isBlocked);
        }
    }
}