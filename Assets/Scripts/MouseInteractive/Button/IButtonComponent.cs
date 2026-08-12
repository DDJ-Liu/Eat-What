public interface IButtonComponent
{
    Button_MouseInteract button { get; set; }
    bool isPressing { get; set; }
    //bool delayedReady { get; set; }
    void setHighlight();
    void setIdle();
    void OnPress();
}
