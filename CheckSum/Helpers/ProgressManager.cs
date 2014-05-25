using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CheckSum.Helpers
{
    public class ProgressManager:IEnumerable<ProgressItem>
    {
        private ProgressManager()
        {
            Items=new List<ProgressItem>();
            ItemsDictionary=new ConcurrentDictionary<string, ProgressItem>();
        }

        public event ProgressUpdatedHandler ProgressUpdated;

        private List<ProgressItem> Items { get; set; }

        private ConcurrentDictionary<string, ProgressItem> ItemsDictionary { get; set; }

        public ProgressItem this[String id]
        {
            get
            {
                return ItemsDictionary.GetOrAdd(id,
                    key =>
                    {
                        var i = new ProgressItem(key);
                        i.ProgressUpdated += x => { if (ProgressUpdated != null) ProgressUpdated(x); };
                        lock(_lock)
                            Items.Add(i);
                        return i;
                    }
                );
            }
        }

        static object _lock = new object();

        public static void RenderProgressToConsole(ProgressItem item)
        {
            bool itemHasBeenRendered = item.LastRenderTime != null;
            bool thresholdedItem = item.DisplayThresholdDuration > TimeSpan.Zero;
            bool itemExceedThreshold = item.Duration > item.DisplayThresholdDuration;
            bool needRender = item.LastRenderTime != null &&
                               DateTime.UtcNow.Subtract(item.LastRenderTime.Value).TotalSeconds >= 1;

            bool forceRender = item.GlobalLastRenderTime != null &&
                               DateTime.UtcNow.Subtract(item.GlobalLastRenderTime.Value).TotalSeconds >= 1;

            if (   (!thresholdedItem && (item.IsComplete || !item.Rendered || needRender))
                || (thresholdedItem && (    
                                            (itemExceedThreshold && !itemHasBeenRendered)
                                         || (itemHasBeenRendered && (item.IsComplete||needRender) )
                                       )
                   )
                )
            {
                lock (_lock)
                {
                    var lineIndex=!item.Rendered?(int?)null:item.LineIndex;
                    
                    Logger.Progress(item.GetProgressMessage(), ref lineIndex);
                    
                    item.LineIndex = lineIndex.GetValueOrDefault();

                    item.LastRenderTime = DateTime.UtcNow;
                    item.GlobalLastRenderTime = item.LastRenderTime;
                    item.Rendered = true;
                    item.DeleteFlag = false;
                }
            }
        }


        public IEnumerator<ProgressItem> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        private static readonly ProgressManager StaticProgressManager=new ProgressManager();
        public static ProgressManager Current { get { return StaticProgressManager; } }
    }
}