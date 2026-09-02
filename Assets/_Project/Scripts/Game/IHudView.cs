namespace CleanPath.Game
{
    public interface IHudView
    {
        void Bind(GameFlow flow);
        void Refresh();
        void LateRefresh();
    }
}
