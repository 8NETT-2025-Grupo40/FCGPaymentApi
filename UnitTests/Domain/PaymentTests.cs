using Fcg.Payment.Domain.Common;
using Fcg.Payment.Domain.Payments;

namespace UnitTests.Domain
{
    public class PaymentTests
    {
        [Fact]
        public void Create_WithValidUserId_ShouldSetPropertiesCorrectly()
        {
            var userId = Guid.NewGuid();
            var currency = "usd";

            var payment = Payment.Create(userId, currency);

            Assert.Equal(userId, payment.UserId);
            Assert.Equal("USD", payment.Currency);
            Assert.Equal(PaymentStatus.Pending, payment.Status);
            Assert.Empty(payment.Items);
        }

        [Fact]
        public void Create_WithEmptyUserId_ShouldThrowDomainException()
        {
            Assert.Throws<DomainException>(() => Payment.Create(Guid.Empty));
        }

        [Fact]
        public void AddItem_WithValidData_ShouldAddItem()
        {
            var payment = Payment.Create(Guid.NewGuid());
            payment.AddItem("game1", 10.5m);

            Assert.Single(payment.Items);
            Assert.Equal(10.5m, payment.Amount);
        }

        [Fact]
        public void AddItem_WithEmptyGameId_ShouldThrowDomainException()
        {
            var payment = Payment.Create(Guid.NewGuid());
            Assert.Throws<DomainException>(() => payment.AddItem("", 10m));
        }

        [Fact]
        public void AddItem_WithNegativeUnitPrice_ShouldThrowDomainException()
        {
            var payment = Payment.Create(Guid.NewGuid());
            Assert.Throws<DomainException>(() => payment.AddItem("game1", -1m));
        }

        [Fact]
        public void MarkAsAuthorized_WhenPending_ShouldSetStatusAndReturnTrue()
        {
            var payment = Payment.Create(Guid.NewGuid());
            var result = payment.MarkAsAuthorized("psp123");

            Assert.True(result);
            Assert.Equal(PaymentStatus.Authorized, payment.Status);
            Assert.Equal("psp123", payment.PspReference);
        }

        [Fact]
        public void MarkAsAuthorized_WhenAlreadyAuthorized_ShouldReturnFalse()
        {
            var payment = Payment.Create(Guid.NewGuid());
            payment.MarkAsAuthorized("psp123");
            var result = payment.MarkAsAuthorized("psp123");

            Assert.False(result);
        }

        [Fact]
        public void MarkAsAuthorized_WhenNotPending_ShouldReturnFalse()
        {
            var payment = Payment.Create(Guid.NewGuid());
            payment.MarkAsAuthorized("psp123");
            payment.MarkAsCaptured("psp123");
            var result = payment.MarkAsAuthorized("psp123");

            Assert.False(result);
        }

        [Fact]
        public void MarkAsCaptured_WhenPendingOrAuthorized_ShouldSetStatusAndReturnTrue()
        {
            var payment = Payment.Create(Guid.NewGuid());
            payment.MarkAsAuthorized("psp123");
            var result = payment.MarkAsCaptured("psp123");

            Assert.True(result);
            Assert.Equal(PaymentStatus.Captured, payment.Status);
        }

        [Fact]
        public void MarkAsCaptured_WhenAlreadyCaptured_ShouldReturnFalse()
        {
            var payment = Payment.Create(Guid.NewGuid());
            payment.MarkAsAuthorized("psp123");
            payment.MarkAsCaptured("psp123");
            var result = payment.MarkAsCaptured("psp123");

            Assert.False(result);
        }

        [Fact]
        public void MarkAsCaptured_WhenFailedOrRefunded_ShouldReturnFalse()
        {
            var payment = Payment.Create(Guid.NewGuid());
            payment.MarkAsFailed("fail", "psp123");
            var result = payment.MarkAsCaptured("psp123");

            Assert.False(result);

            var payment2 = Payment.Create(Guid.NewGuid());
            payment2.MarkAsAuthorized("psp123");
            payment2.MarkAsCaptured("psp123");
            payment2.MarkAsRefunded("psp123");
            var result2 = payment2.MarkAsCaptured("psp123");

            Assert.False(result2);
        }

        [Fact]
        public void MarkAsFailed_WhenNotFailedOrCapturedOrRefunded_ShouldSetStatusAndReturnTrue()
        {
            var payment = Payment.Create(Guid.NewGuid());
            var result = payment.MarkAsFailed("fail", "psp123");

            Assert.True(result);
            Assert.Equal(PaymentStatus.Failed, payment.Status);
        }

        [Fact]
        public void MarkAsFailed_WhenAlreadyFailed_ShouldReturnFalse()
        {
            var payment = Payment.Create(Guid.NewGuid());
            payment.MarkAsFailed("fail", "psp123");
            var result = payment.MarkAsFailed("fail", "psp123");

            Assert.False(result);
        }

        [Fact]
        public void MarkAsFailed_WhenCapturedOrRefunded_ShouldReturnFalse()
        {
            var payment = Payment.Create(Guid.NewGuid());
            payment.MarkAsAuthorized("psp123");
            payment.MarkAsCaptured("psp123");
            var result = payment.MarkAsFailed("fail", "psp123");

            Assert.False(result);

            var payment2 = Payment.Create(Guid.NewGuid());
            payment2.MarkAsAuthorized("psp123");
            payment2.MarkAsCaptured("psp123");
            payment2.MarkAsRefunded("psp123");
            var result2 = payment2.MarkAsFailed("fail", "psp123");

            Assert.False(result2);
        }

        [Fact]
        public void MarkAsRefunded_WhenCaptured_ShouldSetStatusAndReturnTrue()
        {
            var payment = Payment.Create(Guid.NewGuid());
            payment.MarkAsAuthorized("psp123");
            payment.MarkAsCaptured("psp123");
            var result = payment.MarkAsRefunded("psp123");

            Assert.True(result);
            Assert.Equal(PaymentStatus.Refunded, payment.Status);
        }

        [Fact]
        public void MarkAsRefunded_WhenAlreadyRefunded_ShouldReturnFalse()
        {
            var payment = Payment.Create(Guid.NewGuid());
            payment.MarkAsAuthorized("psp123");
            payment.MarkAsCaptured("psp123");
            payment.MarkAsRefunded("psp123");
            var result = payment.MarkAsRefunded("psp123");

            Assert.False(result);
        }

        [Fact]
        public void MarkAsRefunded_WhenNotCaptured_ShouldReturnFalse()
        {
            var payment = Payment.Create(Guid.NewGuid());
            var result = payment.MarkAsRefunded("psp123");

            Assert.False(result);
        }

        [Fact]
        public void BindPspReference_WhenReferenceIsSet_ShouldNotThrowIfSame()
        {
            var payment = Payment.Create(Guid.NewGuid());
            payment.MarkAsAuthorized("psp123");
            payment.BindPspReference("psp123");
            Assert.Equal("psp123", payment.PspReference);
        }

        [Fact]
        public void BindPspReference_WhenReferenceIsConflicting_ShouldThrow()
        {
            var payment = Payment.Create(Guid.NewGuid());
            payment.MarkAsAuthorized("psp123");
            Assert.Throws<InvalidOperationException>(() => payment.BindPspReference("psp456"));
        }

        [Fact]
        public void BindPspReference_WhenReferenceIsEmpty_ShouldThrow()
        {
            var payment = Payment.Create(Guid.NewGuid());
            Assert.Throws<ArgumentException>(() => payment.BindPspReference(""));
        }
    }
}
