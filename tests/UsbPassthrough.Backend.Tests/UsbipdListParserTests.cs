using UsbPassthrough.Backend;

namespace UsbPassthrough.Backend.Tests;

[TestClass]
public sealed class UsbipdListParserTests
{
    [TestMethod]
    public void Parse_ConnectedDevices_ReturnsRows()
    {
        var sample = """
            Connected:
            BUSID  VID:PID    DEVICE                                                        STATE
            2-2    046D:C534  USB Input Device                                              Shared
            4-4    04B4:00F1  Cypress USB                                                    Attached

            Persisted:
            GUID                                  DEVICE
            """;

        var result = UsbipdListParser.Parse(sample);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("2-2", result[0].DeviceId);
        Assert.AreEqual("046D:C534", result[0].VidPid);
        Assert.AreEqual("Shared", result[0].Status);
        Assert.AreEqual("external-client", result[1].AttachedVm);
    }
}