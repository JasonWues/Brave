namespace Brave.scripts.Interface;

public interface IStateHandler
{
    public void TransitionState(int currentState,int newState);
    
    public int GetNextState(int currentState);

    public void TickPhysics(int currentState,double delta);
}