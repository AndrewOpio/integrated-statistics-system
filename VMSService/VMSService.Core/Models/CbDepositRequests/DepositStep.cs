namespace VMSService.Core.Models.CbRequests
{
    public class DepositStep
    {

        public string Key { get; init; } = string.Empty;
        public string Icon { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public string SubLabel { get; init; } = string.Empty;
        public StepState State { get; init; }
        public bool IsVisible { get; init; }
        public bool IsClickable => State != StepState.InProcess;
        public bool IsConnectorDone { get; init; }
        public bool ShowCheckIcon => State == StepState.Done;
        public string CssState => State.ToString().ToLower(); 
    }
}
