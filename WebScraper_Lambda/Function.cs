using Amazon.Lambda.Core;
using HtmlAgilityPack;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace WebScraper_Lambda;

public class Function
{
    
    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input"></param>
    /// <param name="context"></param>
    /// <returns></returns>
   
    //original
    //public string FunctionHandler(MyData data, ILambdaContext context)
    //{
    //    List<Rental> rentalList = GetHTMLAsync();

    //    return $"Hello {data.Name}".ToUpper();
    //}

    //refactor
    public List<Rental> FunctionHandler()
    {
        List<Rental> rentalList = GetHTMLAsync();

        return rentalList;
    }

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

    public class MyData
    {
        public MyData(string name)
        {
            Name = name;
        }
        public string Name { get; set; }
    }
}


//https://www.youtube.com/watch?v=IHIJFVUQyFY&list=PL59L9XrzUa-nYfftB6rfzo-GFCIbufdVO
