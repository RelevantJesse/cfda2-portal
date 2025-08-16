//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Configuration;
//using Portal.Server.Data;
//using Portal.Shared.Models;
//using Stripe;
//using System;
//using System.IO;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;

//namespace Portal.Server.Controllers;

//[ApiController]
//[Route("api/webhooks/stripe")]
//public class WebhooksController : ControllerBase
//{
//    private readonly IConfiguration _configuration;
//    private readonly AppDbContext _db;
//    public WebhooksController(IConfiguration configuration, AppDbContext db)
//    {
//        _configuration = configuration;
//        _db = db;
//    }

//    [HttpPost]
//    public async Task<IActionResult> Handle()
//    {
//        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
//        var secret = _configuration.GetValue<string>("Stripe:WebhookSecret");
//        try
//        {
//            var signatureHeader = Request.Headers["Stripe-Signature"];
//            var stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, secret);
//            if (stripeEvent.Type == Events.PaymentIntentSucceeded)
//            {
//                var intent = stripeEvent.Data.Object as PaymentIntent;
//                if (intent != null)
//                {
//                    var payment = await _db.Payments.FirstOrDefaultAsync(p => p.ProcessorPaymentIntentId == intent.Id);
//                    if (payment != null)
//                    {
//                        payment.Status = intent.Status;
//                        _db.LedgerEntries.Add(new LedgerEntry
//                        {
//                            FamilyId = payment.FamilyId,
//                            PostedUtc = DateTime.UtcNow,
//                            Type = LedgerEntryType.Credit,
//                            AmountCents = payment.AmountCents,
//                            Memo = "Stripe payment succeeded"
//                        });
//                        await _db.SaveChangesAsync();
//                    }
//                }
//            }
//            else if (stripeEvent.Type == Events.PaymentIntentPaymentFailed)
//            {
//                var intent = stripeEvent.Data.Object as PaymentIntent;
//                if (intent != null)
//                {
//                    var payment = await _db.Payments.FirstOrDefaultAsync(p => p.ProcessorPaymentIntentId == intent.Id);
//                    if (payment != null)
//                    {
//                        payment.Status = intent.Status;
//                        await _db.SaveChangesAsync();
//                    }
//                }
//            }
//            return Ok();
//        }
//        catch (Exception)
//        {
//            return BadRequest();
//        }
//    }
//}