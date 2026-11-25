using TMPro;
using UnityEngine;

public class ARModelInfo : MonoBehaviour
{
    public Transform imageTarget;
    public TMP_Text informationText;
    public TMP_Text titleText;

    private Transform previousModel;
    private int previousCount = -1;

    private void LateUpdate()
    {
        if (imageTarget == null || informationText == null)
            return;

        Transform selected = null;
        int selectedIndex = -1;
        int count = imageTarget.childCount;

        for (int i = 0; i < count; i++)
        {
            Transform child = imageTarget.GetChild(i);

            if (child.gameObject.activeSelf)
            {
                selected = child;
                selectedIndex = i;
                break;
            }
        }

        if (selected == previousModel && count == previousCount)
            return;

        previousModel = selected;
        previousCount = count;

        if (selected == null)
        {
            informationText.text = "";
            return;
        }

        string title;
        string description;

        switch (selected.name)
        {
            case "Sun":
                title = "Sol";
                description = "La estrella alrededor de la cual orbitan los planetas.";
                break;

            case "Mercury":
                title = "Mercurio";
                description = "El planeta más cercano al Sol.";
                break;

            case "Venus":
                title = "Venus";
                description = "Un planeta rocoso con una atmósfera muy densa.";
                break;

            case "Earth":
                title = "Tierra";
                description = "Nuestro planeta: océanos, continentes y vida.";
                break;

            case "Mars":
                title = "Marte";
                description = "El planeta rojo, explorado mediante robots.";
                break;

            case "Jupiter":
                title = "Júpiter";
                description = "El planeta más grande del sistema solar.";
                break;

            case "Saturn":
                title = "Saturno";
                description = "Sus anillos contienen partículas de hielo y roca.";
                break;

            case "Uranus":
                title = "Urano";
                description = "Un gigante helado con un eje de rotación muy inclinado.";
                break;

            case "Neptune":
                title = "Neptuno";
                description = "El planeta más lejano del Sol entre los ocho planetas.";
                break;

            case "AR_Astronauta_Finn":
                title = "Explorador Finn";
                description = "Personaje ficticio animado: observa su saludo.";
                break;

            case "AR_Astronauta_Rae":
                title = "Exploradora Rae";
                description = "Personaje ficticio con animación de reposo.";
                break;

            case "AR_Mech_Barbara":
                title = "Mech Barbara";
                description = "Robot ficticio que reproduce una animación de baile.";
                break;

            case "AR_Alien_Volador":
                title = "Alien volador";
                description = "Criatura ficticia con animación de vuelo.";
                break;

            default:
                title = selected.name;
                description = "Modelo presentado en realidad aumentada.";
                break;
        }

        if (titleText != null)
        {
            titleText.text = title.ToUpperInvariant();
            informationText.text = description;
        }
        else
        {
            informationText.text = $"<b>{title}</b>\n{description}";
        }
    }
}
