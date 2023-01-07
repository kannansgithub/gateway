//https://github.com/mehulmpt/razorpay-payments-tutorial
//https://www.youtube.com/watch?v=QtsvGEB7n0s
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using MyCart.Payment.Models;
using Razorpay.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<KestrelServerOptions>(options =>
    {
        options.AllowSynchronousIO = true;
    });
string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: MyAllowSpecificOrigins,
                          builder =>
                          {
                              builder.WithOrigins("http://localhost:3000",
                                                  "http://www.contoso.com",
                                                  "https://flipkart-clone-kkig.vercel.app");
                          });
    });
var app = builder.Build();
app.UseCors(MyAllowSpecificOrigins);
// app.UseHttpsRedirection();
// [FromHeader(Name = "X-Razorpay-Signature")] string signature
app.MapPost("/payment-verification", (IConfiguration configuration, HttpContext ctx, [FromBody] RazorpayResponse paymentResult, [FromHeader(Name = "X-Razorpay-Signature")] string expectedSignature) =>
{
    try
    {
        var secret = configuration.GetValue<string>("razorpay:hook-secret") ?? string.Empty;
        string orderId = paymentResult?.payload?.payment?.entity?.order_id ?? string.Empty;
        string paymentId = paymentResult?.payload?.payment?.entity?.id ?? string.Empty;
        string payload = string.Format("{0}|{1}", orderId, paymentId);

        Dictionary<string, string> attributes = new Dictionary<string, string>();

        attributes.Add("razorpay_payment_id", paymentId);
        attributes.Add("razorpay_order_id", orderId);
        attributes.Add("razorpay_signature", expectedSignature);

        Utils.verifyPaymentSignature(attributes);
        var signatureVerified = verifySignature(payload, expectedSignature, secret);
        if (signatureVerified)
        {
            Console.WriteLine("Signature Verified Successfully");
        }
        else
        {
            Console.WriteLine("Signature Verification Faild");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }
    return Results.Ok();
});
app.MapPost("/checkout", (IConfiguration configuration) =>
{
    try
    {
        int paymentCapture = 1;
        var id = configuration.GetValue<string>("razorpay:id");
        var secret = configuration.GetValue<string>("razorpay:secret");
        RazorpayClient client = new RazorpayClient(id, secret);
        Dictionary<string, object> options = new Dictionary<string, object>();
        options.Add("amount", 20000);
        options.Add("payment_capture", paymentCapture);
        options.Add("currency", "INR");
        options.Add("receipt", Guid.NewGuid().ToString().Replace("-", ""));
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)(0xc0 | 0x300 | 0xc00);
        ServicePointManager.Expect100Continue = false;
        Order orderResponse = client.Order.Create(options);
        Console.WriteLine(orderResponse);
        var orderId = orderResponse.Attributes["id"].ToString();
        return Results.Ok(new
        {
            id = orderResponse.Attributes["id"].ToString(),
            currency = orderResponse.Attributes["currency"].ToString(),
            amount = orderResponse.Attributes["amount"].ToString()
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message, ex.StackTrace);
        return Results.BadRequest();
    }
});
static bool verifySignature(string payload, string expectedSignature, string secret)
{
    string actualSignature = getActualSignature(payload, secret);
    Console.WriteLine($"actualSignature : {actualSignature}");
    Console.WriteLine($"expectedSignature : {expectedSignature}");
    bool verified = actualSignature.Equals(expectedSignature);

    return verified;
}
static string getActualSignature(string payload, string secret)
{
    byte[] secretBytes = StringEncode(secret);

    HMACSHA256 hashHmac = new HMACSHA256(secretBytes);

    var bytes = StringEncode(payload);

    return HashEncode(hashHmac.ComputeHash(bytes));
}
static byte[] StringEncode(string text)
{
    var encoding = new ASCIIEncoding();
    return encoding.GetBytes(text);
}

static string HashEncode(byte[] hash)
{
    return BitConverter.ToString(hash).Replace("-", "").ToLower();
}

app.Run();
