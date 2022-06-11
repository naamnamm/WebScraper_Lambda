using Amazon.Lambda.Core;
using FluentEmail.Core;
using FluentEmail.Razor;
using FluentEmail.Smtp;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace WebScraper_Lambda;

public class Function
{
    public async Task FunctionHandler()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("config.json", optional: false);

        IConfiguration config = builder.Build();

        var mail = config.GetSection("Mail").Get<Mail>();

        List<Rental> rentalList = GetHTMLAsync();

        await sendEmail(rentalList, mail);
    }

    private static async Task sendEmail(List<Rental> rentalList, Mail mail)
    {
        try
        {
            var sender = new SmtpSender(() => new SmtpClient()
            {
                //sender pondpattohs@gmail.com connect to gmail server
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                EnableSsl = true,
                Host = "smtp.mail.yahoo.com",
                Port = 587,
                Credentials = new NetworkCredential(mail.Username, mail.Password)

            });

            var template = @"  
                    Hey @Model.Name, here is your daily rental list. 
                    @for(var i = 1; i < @Model.RentalList.Count; i++) 
                    {                        
                        <p>
                            <b>Item:</b> @i
                            <br>
                            <b>Title:</b> @Model.RentalList[i].Title 
                            <br>
                            <b>Location:</b> @Model.RentalList[i].Location 
                            <br>
                            <b>SquareFootage:</b> @Model.RentalList[i].SquareFootage 
                            <br>
                            <b>Price:</b> @Model.RentalList[i].Price
                            <br>
                            <b>Link:</b> @Model.RentalList[i].Link
                        </p>
                    }

                ";

            var model = new { Name = "Naam", RentalList = rentalList };

            Email.DefaultSender = sender;
            Email.DefaultRenderer = new RazorRenderer();

            var email = await Email
                .From("naam.namm@yahoo.com")
                .To("naam.pt@gmail.com", "Naam")
                .Subject("Daily Rental List")
                .UsingTemplate(template, model)
                .SendAsync();

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    //public List<Rental> FunctionHandler()
    //{
    //    List<Rental> rentalList = GetHTMLAsync();

    //    return rentalList;
    //}

    private static List<Rental> GetHTMLAsync()
    {
        var url = new URI("apartment", "1300", "1800", "0").ToString();

        var httpClient = new HttpClient();
        var task = httpClient.GetStringAsync(url);

        var html = task.GetAwaiter().GetResult();

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);

        var rentalsHtml = htmlDocument.DocumentNode.Descendants("ul")
            .Where(n => n.GetAttributeValue("id", "")
            .Equals("search-results")).ToList();

        var rentalList = rentalsHtml[0].Descendants("li")
            .Where(n => n.GetAttributeValue("data-pid", "").StartsWith("7")).ToList();

        return parseHTMLNodeList(rentalList);

    }

    private static List<Rental> parseHTMLNodeList(List<HtmlNode> rentalList)
    {
        List<Rental> rentals = new List<Rental>();


        // loop through rentalList (from HTML), get info, then add it to a rentals
        foreach (var rental in rentalList)
        {
            var resultUrl = rental.Descendants("a").FirstOrDefault()?
            .GetAttributeValue("href", "") ??
            String.Empty;

            if (!resultUrl.Contains("washingtondc")) return rentals;

            var rentalID = rental.GetAttributeValue("data-pid", "");

            var rentalTitle = rental.Descendants("h3")
            .Where(n => n.GetAttributeValue("class", "").Equals("result-heading")).FirstOrDefault()?
            .InnerText.Trim() ??
            String.Empty;

            var rentalLocation = rental.Descendants("span")
            .Where(n => n.GetAttributeValue("class", "").Equals("result-hood")).FirstOrDefault()?
            .InnerText.Trim().Trim('(', ')') ??
            String.Empty;

            var rentalSquarefootage = rental.Descendants("span")
            .Where(n => n.GetAttributeValue("class", "").Equals("housing")).FirstOrDefault()?
            .InnerText.Replace("\n", String.Empty).Replace(" ", "") ??
            String.Empty;

            string trimmedRentalSqFt = rentalSquarefootage.Length > 0 ?
                rentalSquarefootage.Remove(rentalSquarefootage.Length - 1) : String.Empty;

            var rentalPrice = rental.Descendants("span")
            .Where(n => n.GetAttributeValue("class", "").Equals("result-price")).FirstOrDefault()?
            .InnerText.Trim() ??
            String.Empty;

            rentals.Add(new Rental(rentalID, rentalTitle, rentalLocation, trimmedRentalSqFt, rentalPrice, resultUrl));

        }

        return rentals;
    }

}


