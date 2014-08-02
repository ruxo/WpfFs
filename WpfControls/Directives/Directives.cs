using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfFs.Directives{
    public class Ng : Control{
        public static void SetBlockMark(DependencyObject el, bool value){
            var ui = el as UIElement;
            if (ui == null)
                return;
            if (value)
                ui.MouseRightButtonDown += onMouseRightButtonDown;
            else
                ui.MouseRightButtonDown -= onMouseRightButtonDown;
        }
        static void onMouseRightButtonDown(object sender, MouseButtonEventArgs e){
            Console.WriteLine("Source = {0}, OriginalSource = {1} @ {2}", e.Source.GetType().Name, e.OriginalSource.GetType().Name, e.Timestamp);

            var source = e.OriginalSource as Control;
            if (source == null)
                return;
            var showThickness = new Thickness(5);
            if (source.BorderThickness != showThickness){
                source.BorderThickness = showThickness;
                source.BorderBrush = Brushes.Black;
            }else
                source.BorderThickness = new Thickness(0);
        }
    }
}