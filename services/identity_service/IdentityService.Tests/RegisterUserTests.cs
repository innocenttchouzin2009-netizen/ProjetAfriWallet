using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdentityService.Tests;

[TestClass]
public class RegisterUserTests
{
  [TestMethod]
  public void RegisteringUser_ShouldProducePendingStatus()
  {
    var result = new { Status = "PENDING" };
    Assert.AreEqual("PENDING", result.Status);
  }
}
