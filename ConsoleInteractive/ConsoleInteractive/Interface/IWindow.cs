using ConsoleInteractive.Interface.Abstract;

namespace ConsoleInteractive.Interface; 

public interface IWindow {
    
    public int Width { get; set; }
    public int Height { get; set; }
    bool IsPrefixVisible { get; set; }
    bool IsOutputVisible { get; set; }
    
    public void Clear();
    public void ClearLine();
    
    public void SetCursorPos(int x, int y);
    public void SetCursorPos(Cursor cursor);
    
    public void SetCursorVisible(bool visible);
}