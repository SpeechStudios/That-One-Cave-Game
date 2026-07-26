using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TabButton : MonoBehaviour
{
    public Image BG;
    public TextMeshProUGUI Text;

    public Color SelectColorImage;
    public Color SelectColorText;

    public Color DeselectColorImage;
    public Color DeselectColorText;

    public void Select()
    {
        BG.color = SelectColorImage;
        Text.color = SelectColorText;
    }
    public void Deselect()
    {
        BG.color = DeselectColorImage;
        Text.color = DeselectColorText;
    }
}
