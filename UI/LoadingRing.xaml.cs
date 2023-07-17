using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Archiver.UI
{
    /// <summary>
    /// Interaction logic for LoadingRing.xaml
    /// </summary>
    public partial class LoadingRing : UserControl
    {
        public LoadingRing()
        {
            InitializeComponent();
            this.Loaded += (s, e) => {
                Storyboard storyboard = (Storyboard)this.Resources["storyBoard"];
                DoubleAnimationUsingKeyFrames animation = (DoubleAnimationUsingKeyFrames)storyboard.Children[0];
                // Storyboard.SetTarget(animation, globalRotate);
                Storyboard.SetTargetProperty(animation, new PropertyPath("(RotateTransform.Angle)"));
                BeginStoryboard(storyboard);
            };
        }
    }
}
