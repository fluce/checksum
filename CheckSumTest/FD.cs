using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CheckSumTest
{
    public static class FD
    {
        public static FDDirectory Directory(params FDItem[] children)
        {
            return Directory(null, children);
        }

        public static FDDirectory Directory(string name, params FDItem[] children)
        {
            var ret = new FDDirectory() {Name = name, Parent = null};
            ret.Children = children.Select(x =>
            {
                x.Parent = ret;
                return x;
            }).ToList();
            return ret;
        }

        public static FDFile File(string name, string content)
        {
            var ret = new FDFile() { Name = name, Parent = null, Content=Encoding.UTF8.GetBytes(content) };
            return ret;
        }

        public abstract class FDItem
        {
            public string Name { get; set; }
            public FDDirectory Parent { get; set; }

            public string FullName
            {
                get
                {
                    if (Parent == null)
                        return Name;
                    return Path.Combine(Parent.FullName, Name);
                }
            }

            public FDItem SetName(string newName)
            {
                Name = newName;
                return this;
            }

            public abstract FDItem Create();
            public abstract FDItem Delete();
            protected abstract FDItem InternalClone();

            public FDItem Clone()
            {
                return InternalClone();
            }

            public FDDirectory AsDirectory()
            {
                return this as FDDirectory;
            }
            public FDFile AsFile()
            {
                return this as FDFile;
            }
        }

        public class FDDirectory : FDItem
        {
            public List<FDItem> Children { get; set; }

            public FDDirectory CreateFromScratch()
            {
                if (System.IO.Directory.Exists(FullName))
                    System.IO.Directory.Delete(FullName,true);
                Create();
                return this;
            }

            public override FDItem Create()
            {
                System.IO.Directory.CreateDirectory(FullName);
                foreach (var item in Children)
                {
                    item.Parent = this;
                    item.Create();
                }
                return this;
            }

            public override FDItem Delete()
            {
                foreach (var item in Children)
                {
                    item.Delete();
                }
                if (System.IO.Directory.EnumerateFileSystemEntries(FullName).Count()==0 )
                    System.IO.Directory.Delete(FullName);
                return this;
            }

            public new FDDirectory Clone()
            {
                return Directory(Name, Children.Select(x => x.Clone()).ToArray());
            }

            protected override FDItem InternalClone()
            {
                return Clone();
            }

            public new FDDirectory SetName(string newName)
            {
                Name = newName;
                return this;
            }

            public FDDirectory SetCurrent()
            {
                System.IO.Directory.SetCurrentDirectory(FullName);
                return this;
            }

            public FDItem this[string name]
            {
                get { return Children.FirstOrDefault(x => x.Name == name); }
            }
        }

        public class FDFile : FDItem
        {
            public byte[] Content { get; set; }
            public TimeSpan Offset { get; set; }

            public override FDItem Create()
            {
                System.IO.File.WriteAllBytes(FullName,Content);
                if (Offset!=TimeSpan.Zero)
                    System.IO.File.SetLastWriteTime(FullName, System.IO.File.GetLastWriteTime(FullName).Add(Offset));
                return this;
            }

            public override FDItem Delete()
            {
                System.IO.File.Delete(FullName);
                return this;
            }

            public new FDFile Clone()
            {
                return new FDFile() { Name = Name, Parent = null, Content = Content, Offset = Offset};
            }

            protected override FDItem InternalClone()
            {
                return Clone();
            }

            public FDFile OffsetDate(TimeSpan timeSpan)
            {
                Offset += timeSpan;
                if (System.IO.File.Exists(FullName))
                    System.IO.File.SetLastWriteTime(FullName, System.IO.File.GetLastWriteTime(FullName).Add(Offset));
                return this;
            }

            public FDFile SetContent(string newContent)
            {
                Content = Encoding.UTF8.GetBytes(newContent);
                return this;
            }
        }
    }
}