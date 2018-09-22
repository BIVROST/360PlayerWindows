namespace Bivrost.Bivrost360Player
{
    public interface IControllableScene
    {
        bool HasFocus { get; set; }
        void ChangeFov(float deg);
        void MoveDelta(float x, float y, float ratio, float lerpSpeed);
        void RectlinearProjection();
        void ResetFov();
        void StereographicProjection();
    }
}
