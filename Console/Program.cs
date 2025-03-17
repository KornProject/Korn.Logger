using Korn;

try
{
    var logger = new KornLogger("temp.log");
    logger.Clear();

    var thread1 = new Thread(Body);
    var thread2 = new Thread(Body);

    thread1.Start();
    thread2.Start();

    thread1.Join();
    thread2.Join();

    void Body()
    {
        for (var i = 0; i < 50000; i++)
            logger.WriteMessage(i.ToString());
    }
} 
catch (Exception ex)
{
    Console.WriteLine(ex);
}

Console.WriteLine("end");
Console.ReadLine();