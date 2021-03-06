# WpfParallelDataflowInvestigation

I created this repository to investigate an issue I'm running into with ordered results and TPL Dataflow. I'm looping through 100 items with DataFlow where item 5 takes 60 seconds (the rest only a few seconds).

This means that result 0,1,2,3,4 are handled and then the wait starts for item 5. While this is happening I can see that the items till 14 will be executed.

```
Posting: 0
>>>>> Threads: 0     In: 0 Out: 0      >>    In: 0 Out: False
Posting: 1
Posting: 2
Posting: 3
Posting: 4
Posting: 5
Posting: 6
Posting: 7
Posting: 8
Posting: 9
Posting: 10
>>>>> Threads: 8     In: 2 Out: 0      >>    In: 0 Out: False
>>>>> Threads: 8     In: 2 Out: 0      >>    In: 0 Out: False
>>>>> Threads: 8     In: 2 Out: 0      >>    In: 0 Out: False
Processed: 4
>>>>> Threads: 8     In: 1 Out: 0      >>    In: 0 Out: False
Processed: 7
>>>>> Threads: 8     In: 0 Out: 0      >>    In: 0 Out: False
>>>>> Threads: 8     In: 0 Out: 0      >>    In: 0 Out: False
Processed: 0
Posting: 11
>>>>> Threads: 8     In: 0 Out: 0      >>    In: 0 Out: False
>>>>> Threads: 8     In: 0 Out: 0      >>    In: 0 Out: False
Processed: 3
Processed: 9
>>>>> Threads: 6     In: 0 Out: 0      >>    In: 0 Out: False
Processed: 8
Processed: 6
Processed: 2
Processed: 1
Posting: 12
Posting: 13
Posting: 14
Posting: 15
>>>>> Threads: 6     In: 0 Out: 0      >>    In: 0 Out: False
>>>>> Threads: 6     In: 0 Out: 0      >>    In: 0 Out: False
Processed: 10
>>>>> Threads: 5     In: 0 Out: 0      >>    In: 0 Out: False
>>>>> Threads: 5     In: 0 Out: 0      >>    In: 0 Out: False
>>>>> Threads: 5     In: 0 Out: 0      >>    In: 0 Out: False
Processed: 13
>>>>> Threads: 4     In: 0 Out: 0      >>    In: 0 Out: False
>>>>> Threads: 4     In: 0 Out: 0      >>    In: 0 Out: False
Processed: 11
Processed: 14
>>>>> Threads: 2     In: 0 Out: 0      >>    In: 0 Out: False
Processed: 12
>>>>> Threads: 1     In: 0 Out: 0      >>    In: 0 Out: False
>>>>> Threads: 1     In: 0 Out: 0      >>    In: 0 Out: False
>>>>> Threads: 1     In: 0 Out: 0      >>    In: 0 Out: False
>>>>> Threads: 1     In: 0 Out: 0      >>    In: 0 Out: False
```

As can be seen in the log file above it ultimately ends with 1 thread running.

### Questions

1. I've set the bound capacity of this block to 10. But does this bound capacity means that it will store 10 items in some kind of output buffer?
2. It also seems the bounded capacity influences the number of items that can be queued (as can be seen at the start where 10 items get queued).
3. I also log the In: .. and Out: .. buffer counts, but they stay 0 all the time. Where are my messages that are done processing but are waiting to be output untill that item 5 is complete?

### The important code:

```
public async Task<IEnumerable<string>> ProcessInParallel(IOutputWriter logListener, int maxDegreeOfParallelism = 8)
{
    var theResults = new List<string>();

    var processBlock = new TransformBlock<int, string>(async number =>
    {
        var processedNumber = await ProcessNumber(number);

        return processedNumber;
    }, new ExecutionDataflowBlockOptions()
    {
        MaxDegreeOfParallelism = maxDegreeOfParallelism,
        BoundedCapacity = 10
    });

    var putInListBlock = new ActionBlock<string>(t =>
    {

        if (logListener != null)
        {
            logListener.WriteOutput(t);
        }
        theResults.Add(t);
    }, SynchronizeForUiThread(new ExecutionDataflowBlockOptions()
    {

    }));

    processBlock.LinkTo(putInListBlock, new DataflowLinkOptions() { PropagateCompletion = true });


    var ttt = Task.Run(async () =>
    {
        while (true)
        {
            Console.WriteLine($">>>>> Threads: {currentThreadsRunning}     In: {processBlock.InputCount} Out: {processBlock.OutputCount}      >>    In: {putInListBlock.InputCount} Out: {putInListBlock.Completion.IsCompleted}");
            await Task.Delay(1000);
        }
    });

    var numbers = EnumerateNumberList(100);
    await Task.Run(async () =>
    {
        foreach (var number in numbers)
        {
            Console.WriteLine($"Posting: {number}");
            var result = await processBlock.SendAsync(number);

            if (!result)
            {
                Console.WriteLine("Result is false!!!");
            }

        }
    });



    Console.WriteLine("Completing");
    processBlock.Complete();
    await putInListBlock.Completion;

    return theResults;
}
```        
