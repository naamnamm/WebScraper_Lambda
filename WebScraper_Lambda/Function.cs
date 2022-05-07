using Amazon.Lambda.Core;

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
    public string FunctionHandler(MyData data, ILambdaContext context)
    {
        return $"Hello {data.Name}".ToUpper();
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
