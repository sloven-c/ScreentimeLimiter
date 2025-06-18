using CommunityToolkit.Mvvm.ComponentModel;

namespace ScreentimeLimiter.ViewModels;

public partial class MainWindowViewModel : ViewModelBase {
    [ObservableProperty] private string _confirmButtonMessage;
    
    public string ExactTimeCheckbox => "Exact time";
    public string AbsoluteTimeMessage => "Set exact time";
    public string RelativeTimeMessage => "Set countdown time";

    public MainWindowViewModel() {
        ConfirmButtonMessage = RelativeTimeMessage;
    }
}