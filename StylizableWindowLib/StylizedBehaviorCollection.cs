using System.Windows;
using System.Windows.Interactivity;

namespace StylizableWindowLib
{
    public class StylizedBehaviorCollection : FreezableCollection<Behavior>
    {
        protected override Freezable CreateInstanceCore()
        {
            return new StylizedBehaviorCollection();
        }
    }
}
