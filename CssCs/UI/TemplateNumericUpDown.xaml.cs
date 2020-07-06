using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CssCs.UI
{
  /// <summary>
  /// Interaction logic for TemplateNumericUpDown.xaml
  /// </summary>
  public partial class TemplateNumericUpDown : UserControl//, INotifyPropertyChanged
  {
    public static readonly DependencyProperty NumValueProperty = DependencyProperty.Register(
      "NumValue", 
      typeof(int),
      typeof(TemplateNumericUpDown),
      new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MaxProperty = DependencyProperty.Register(
      "Max", 
      typeof(int), 
      typeof(TemplateNumericUpDown),
      new FrameworkPropertyMetadata(100, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MinProperty = DependencyProperty.Register(
      "Min", 
      typeof(int), 
      typeof(TemplateNumericUpDown),
      new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
      "Step",
      typeof(int),
      typeof(TemplateNumericUpDown),
      new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public TemplateNumericUpDown()
    {
      //this.DataContext = this;
      InitializeComponent();
      timer = new System.Timers.Timer(1000);
      timer.AutoReset = false;
      timer.Elapsed += Timer_Elapsed;


      timer2 = new System.Timers.Timer(100);
      timer2.AutoReset = false;
      timer2.Elapsed += Timer2_Elapsed;
    }


    public int NumValue
    {
      get { return (int)GetValue(NumValueProperty); }
      set
      {
        int temp = value;
        if (value > Max) temp = Max;
        else if (value < Min) temp = Min;
        SetValue(NumValueProperty, temp);
      }
    }
    public int Max
    {
      get { return (int)GetValue(MaxProperty); }
      set
      {
        SetValue(MaxProperty, value);
        if (NumValue > value) NumValue = value; 
      }
    }
    public int Min
    {
      get { return (int)GetValue(MinProperty); }
      set
      {
        SetValue(MinProperty, value);
        if (NumValue < value) NumValue = value;
      }
    }

    public int Step
    {
      get
      {
        int step = (int)GetValue(StepProperty);
        if (step < 1) return 1;
        else return step;
      }
    }

    void StepNum()
    {
      if (flag_up) NumValue += Step;
      else NumValue -= Step;
    }

    private bool flag_up { get; set; } = false;


    private void TxtNum_TextChanged(object sender, TextChangedEventArgs e)
    {
      int num;
      if (int.TryParse(txtNum.Text, out num))
      {
        if (num != NumValue) NumValue = num;
      }
      else txtNum.Text = NumValue.ToString();
    }

    System.Timers.Timer timer;
    System.Timers.Timer timer2;
    private void Timer2_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      Dispatcher.Invoke(() =>
      {
        StepNum();
        timer2.Start();
      });      
    }

   

    private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      timer2.Start();
    }

    
    private void EventMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      flag_up = sender.Equals(up);
      StepNum();

      (sender as Grid).Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xC1, 0xB0, 0xE0));//FFC1B0E0
      timer.Start();
    }

    private void EventMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      (sender as Grid).Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x67, 0x3A, 0xB7));//FF673AB7
      timer.Stop();
      timer2.Stop();
    }

    private void EventMouseLeave(object sender, MouseEventArgs e)
    {
      timer.Stop();
      timer2.Stop();
    }

    private void txtNum_FocusableChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      timer.Stop();
      timer2.Stop();
    }

    private void txtNum_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Up || e.Key == Key.Down)
      {
        flag_up = e.Key == Key.Up;
        StepNum();
        timer.Start();
      }
    }

    private void txtNum_PreviewKeyUp(object sender, KeyEventArgs e)
    {
      timer.Stop();
      timer2.Stop();
    }
  }
}
