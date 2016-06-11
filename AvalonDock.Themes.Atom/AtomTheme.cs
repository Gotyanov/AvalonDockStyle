using System;
using Xceed.Wpf.AvalonDock.Themes;

namespace AvalonDock.Themes.Atom
{
    public class AtomTheme: Theme
    {
        public override Uri GetResourceUri()
        {
            return new Uri("/" + GetType().Assembly.GetName().Name + ";component/Theme.xaml", UriKind.Relative);
        }
    }
}
