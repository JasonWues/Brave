using Brave.scripts.Interface;
using Brave.scripts.player;
using Godot;

public partial class Player : CharacterBody2D,IStateHandler
{
    public readonly PlayerMovementState GroundStates =
        PlayerMovementState.Idle | PlayerMovementState.Running | PlayerMovementState.Landing;
    public const float Speed = 160.0f;
    public const float JumpVelocity = -400.0f;
    public const float FloorAcceleration = (float)(Speed / 0.2);
    public const float AirAcceleration = (float)(Speed / 0.02);
    
    private float _defaultGravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    private bool _isFirstTick = false;
    
    private AnimationPlayer _animationPlayer;
    private Sprite2D _sprite2D;
    private Timer _coyoteTimer;
    private Timer _jumpRequestTimer;
    
    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _sprite2D = GetNode<Sprite2D>("Sprite2D");
        _coyoteTimer = GetNode<Timer>("CoyoteTimer");
        _jumpRequestTimer = GetNode<Timer>("JumpRequestTimer");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("jump"))
        {
            _jumpRequestTimer.Start();
        }
        //短摁空格跳的更低，长摁更高
        if (@event.IsActionReleased("jump"))
        {
            _jumpRequestTimer.Stop();
            if (Velocity.Y < JumpVelocity / 2)
            {
                Vector2 velocity = Velocity;
                velocity.Y = JumpVelocity / 2;
                Velocity = velocity;
            }
        }
    }

    public void Move(float gravity,double delta)
    {
        Vector2 velocity = Velocity;
		
        var direction = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");

        var acceleration = IsOnFloor() ? FloorAcceleration : AirAcceleration;
        
        velocity.X = (float)Mathf.MoveToward(velocity.X,direction * Speed,acceleration * delta) ;
        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y += _defaultGravity * (float)delta;

        if (!Mathf.IsZeroApprox(direction))
        {
            _sprite2D.FlipH = direction < 0;
        }
		
        Velocity = velocity;
        MoveAndSlide();
    }

    public void Stand(double delta)
    {
        Vector2 velocity = Velocity;
        
        var acceleration = IsOnFloor() ? FloorAcceleration : AirAcceleration;
        
        velocity.X = (float)Mathf.MoveToward(velocity.X,0.0,acceleration * delta) ;
		
        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y += _defaultGravity * (float)delta;
        
        Velocity = velocity;
        MoveAndSlide();
    }
    
    public void TickPhysics(int currentState, double delta)
    {
        switch (currentState)
        {
            case (int)PlayerMovementState.Idle:
                Move(_defaultGravity,delta);
                break;
            case (int)PlayerMovementState.Running:
                Move(_defaultGravity,delta);
                break;
            case (int)PlayerMovementState.Jump:
                Move(_isFirstTick ? 0.0f : _defaultGravity,delta);
                break;
            case (int)PlayerMovementState.Fall:
                Move(_defaultGravity,delta);
                break;
            case (int)PlayerMovementState.Landing:
                Stand(delta);
                break;
        }
        _isFirstTick = false;
    }

    public void TransitionState(int currentState, int newState)
    {

        if (!GroundStates.HasFlag((PlayerMovementState)currentState) && GroundStates.HasFlag((PlayerMovementState)newState))
        {
            _coyoteTimer.Stop();

        }
        
        switch (newState)
        {
            case (int)PlayerMovementState.Idle:
                _animationPlayer.Play("idle");
                break;
            case (int)PlayerMovementState.Running:
                _animationPlayer.Play("running");
                break;
            case (int)PlayerMovementState.Jump:
                _animationPlayer.Play("jump");
                var velocity = Velocity;
                velocity.Y = JumpVelocity;
                _coyoteTimer.Stop();
                _jumpRequestTimer.Stop();
                Velocity = velocity;
                break;
            case (int)PlayerMovementState.Fall:
                _animationPlayer.Play("fall");
                if (GroundStates.HasFlag((PlayerMovementState)currentState))
                {
                    _coyoteTimer.Start();
                }
                break;
            case (int)PlayerMovementState.Landing:
                _animationPlayer.Play("landing");
                break;
        }
        _isFirstTick = true;
    }
    
    public int GetNextState(int currentState)
    {
        var direction = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
        //是否静止
        var isStill = Mathf.IsZeroApprox(direction) && Mathf.IsZeroApprox(Velocity.X);
        
        var canJump = IsOnFloor() || _coyoteTimer.TimeLeft > 0;
        var shouldJump = canJump && _jumpRequestTimer.TimeLeft > 0;
        if (shouldJump)
        {
            return (int)PlayerMovementState.Jump;
        }
        
        switch (currentState)
        {
            case (int)PlayerMovementState.Idle:
                if (!IsOnFloor())
                {
                    return (int)PlayerMovementState.Fall;
                }
                if (!isStill)
                {
                    return (int)PlayerMovementState.Running;
                }
                break;
            case (int)PlayerMovementState.Running:
                if (!IsOnFloor())
                {
                    return (int)PlayerMovementState.Fall;
                }
                if (isStill)
                {
                    return (int)PlayerMovementState.Idle;
                }
                break;
            case (int)PlayerMovementState.Jump:
                if (Velocity.Y >= 0)
                {
                    return (int)PlayerMovementState.Fall;
                }
                break;
            case (int)PlayerMovementState.Fall:
                if (IsOnFloor())
                {
                    return isStill ? (int)PlayerMovementState.Landing : (int)PlayerMovementState.Running;
                }
                break;
            case (int)PlayerMovementState.Landing:
                if (!_animationPlayer.IsPlaying())
                {
                    return (int)PlayerMovementState.Idle;
                }
                break;
        }
        return currentState;
    }
}
