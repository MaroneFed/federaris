namespace Fief
{
    /// <summary>Un panneau modal (marche, construction, coffre). Le HUD n'en affiche qu'un a la fois.</summary>
    public interface IPanel
    {
        /// <summary>Faux = le panneau se ferme tout seul (on s'est eloigne, par exemple).</summary>
        bool IsStillValid { get; }

        void Draw();
    }
}
