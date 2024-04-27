namespace ChattersInGame.Alerts
{
    public class AlertMessageConstant : AlertMessage
    {
        public readonly string AlertString;

        public AlertMessageConstant(string alertString)
        {
            AlertString = alertString;
        }

        public override string ConstructAlertString()
        {
            return AlertString;
        }
    }
}
