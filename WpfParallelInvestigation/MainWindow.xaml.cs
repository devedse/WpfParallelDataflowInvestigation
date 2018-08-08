using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;

namespace WpfParallelInvestigation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IOutputWriter
    {
        int currentThreadsRunning = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Hallo");

            await ProcessInParallel(this, 8);

        }


        public void WriteOutput(string msg)
        {
            logList.Items.Insert(0, msg);
        }



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


        public IEnumerable<int> EnumerateNumberList(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return i;
            }
        }

        [ThreadStatic]
        public static Random random;

        public async Task<string> ProcessNumber(int number)
        {
            if (random == null)
            {
                random = new Random(Guid.NewGuid().GetHashCode());
            }

            Interlocked.Increment(ref currentThreadsRunning);

            if (number == 5)
            {
                await Task.Delay(60000);
            }
            else
            {
                await Task.Delay(random.Next(3000, 10000));
            }
            Interlocked.Decrement(ref currentThreadsRunning);
            Console.WriteLine($"Processed: {number}");
            return number.ToString();
        }

        public static ExecutionDataflowBlockOptions SynchronizeForUiThread(ExecutionDataflowBlockOptions executionDataflowBlockOptions)
        {
            if (SynchronizationContext.Current != null)
            {
                executionDataflowBlockOptions.TaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            }
            return executionDataflowBlockOptions;
        }
    }
}
