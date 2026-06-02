using System;
using Xunit;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;

namespace OrderingSystem.Tests
{
    public class OrderDomainTests
    {
        [Fact]
        public void NewOrder_ShouldInitializeWithPendingStatusAndCustomerName()
        {
            // Arrange
            string expectedCustomer = "John Doe";

            // Act
            var order = new Order(expectedCustomer); 

            // Assert
            Assert.Equal(OrderStatus.Pending, order.Status);
            Assert.Equal(expectedCustomer, order.CustomerName);
            Assert.NotEqual(Guid.Empty, order.Id);
        }
        [Fact]
        public void SetAISummary_ShouldUpdateAISummaryProperty()
        {
            // Arrange
            var order = new Order("Jane Doe");
            string sampleSummary = "Order contains premium items requiring expedited processing.";

            // Act
            order.SetAISummary(sampleSummary);

            // Assert
            Assert.Equal(sampleSummary, order.AISummary);
        }
        [Fact]
        public void AddItem_ShouldRecalculateTotalAmountCorrectly()
        {
            // Arrange
            var order = new Order("Test Customer");

            // Act
            order.AddItem("Mechanical Keyboard", 120.00m, 1);
            order.AddItem("Ergonomic Mouse", 60.00m, 2);

            // Assert
            // Total should be (120 * 1) + (60 * 2) = 240
            Assert.Equal(240.00m, order.TotalAmount);
            Assert.Equal(2, order.Items.Count);
        }
    }
}