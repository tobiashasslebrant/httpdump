using httpdump;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();
var requestWriter = new ResponseWriter();
app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/cache"))
        await requestWriter.Cache(context); 
    else if(context.Request.Path.Equals("/clear"))
        await requestWriter.Clear(context);
    else
        await requestWriter.Write(context);

    try
    {
        await next();
    }
    catch
    {
        // ignored
    }
});
app.Run();
