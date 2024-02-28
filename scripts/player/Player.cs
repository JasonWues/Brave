using Brave.scripts.Interface;
using Brave.scripts.player;
using Godot;

public partial class Player : CharacterBody2D,IStateHandler
{
    public readonly PlayerMovementState GroundStates =
        PlayerMovementState.Idle | PlayerMovementState.Running | PlayerMovementState.Landing;
    public const float Speed = 160.0f;
    public const float JumpVelocity = -400.0f;
    public Vector2 WallJumpVelocity = new Vector2(450f, -320f);
    public const float FloorAcceleration = Speed / 0.2f;
    public const float AirAcceleration = Speed / 0.1f;
    
    private float _defaultGravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    private bool _isFirstTick = false;
    
    private AnimationPlayer _animationPlayer;
    private RayCast2D _handRay;
    private RayCast2D _footRay;
    private Node2D _graphics;
    private Timer _coyoteTimer;
    private Timer _jumpRequestTimer;
    private StateMachine _stateMachine;
    
    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _graphics = GetNode<Node2D>("Graphics");
        _coyoteTimer = GetNode<Timer>("CoyoteTimer");
        _jumpRequestTimer = GetNode<Timer>("JumpRequestTimer");
        _handRay = GetNode<RayCast2D>("Graphics/HandRay");
        _footRay = GetNode<RayCast2D>("Graphics/FootRay");
        _stateMachine = GetNode<StateMachine>("StateMachine");
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
            _graphics.Scale = _graphics.Scale with
            {
                X = direction < 0 ? -1 : 1
            };
        }
		
        Velocity = velocity;
        MoveAndSlide();
    }

    public void Stand(float gravity,double delta)
    {
        Vector2 velocity = Velocity;
        
        var acceleration = IsOnFloor() ? FloorAcceleration : AirAcceleration;
        
        velocity.X = (float)Mathf.MoveToward(velocity.X,0.0,acceleration * delta) ;
		
        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y += gravity * (float)delta;
        
        Velocity = velocity;
        MoveAndSlide();
    }

    public bool CanWallSlide()
    {
        return IsOnWall() && _handRay.IsColliding() && _footRay.IsColliding();
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
                Stand(_defaultGravity,delta);
                break;
            case (int)PlayerMovementState.WallSliding:
                Move(_defaultGravity /3,delta);
                _graphics.Scale = _graphics.Scale with
                {
                    X = GetWallNormal().X
                };
                break;
            case (int)PlayerMovementState.WallJump:
                if (_stateMachine.StateTime < 0.1)
                {
                    Stand(_isFirstTick ? 0.0f : _defaultGravity,delta);
                    _graphics.Scale = _graphics.Scale with
                    {
                        X = GetWallNormal().X
                    };
                }
                else
                {
                    Move(_isFirstTick ? 0.0f : _defaultGravity,delta);

                }
                break;
        }
        _isFirstTick = false;
    }

    public void TransitionState(int currentState, int newState)
    {

#if DEBUG
        GD.Print($"[{Engine.GetPhysicsFrames()}] {(PlayerMovementState)currentState} => {(PlayerMovementState)newState}");
#endif

        if (!GroundStates.HasFlag((PlayerMovementState)currentState) && GroundStates.HasFlag((PlayerMovementState)newState))
        {
            _coyoteTimer.Stop();

        }
        var velocity = Velocity;
        
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
            case (int)PlayerMovementState.WallSliding:
                _animationPlayer.Play("wallsliding");
                break;
            case (int)PlayerMovementState.WallJump:
                _animationPlayer.Play("jump");
                velocity = WallJumpVelocity;
                velocity.X *= GetWallNormal().X;
                Velocity = velocity;
                _jumpRequestTimer.Stop();
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
                if (CanWallSlide())
                {
                    return (int)PlayerMovementState.WallSliding;
                }
                break;
            
            case (int)PlayerMovementState.Landing:
                if (!isStill)
                {
                    return (int)PlayerMovementState.Running;
                }
                if (!_animationPlayer.IsPlaying())
                {
                    return (int)PlayerMovementState.Idle;
                }
                break;
            
            case (int)PlayerMovementState.WallSliding:
                if (_jumpRequestTimer.TimeLeft > 0)
                {
                    return (int)PlayerMovementState.WallJump;
                }
                if (IsOnFloor())
                {
                    return (int)PlayerMovementState.Idle;
                }
                if (!IsOnWall())
                {
                    return (int)PlayerMovementState.Fall;
                }
                break;
            case (int)PlayerMovementState.WallJump:
                if (CanWallSlide() && !_isFirstTick)
                {
                    return (int)PlayerMovementState.WallSliding;
                }
                if (Velocity.Y >= 0)
                {
                    return (int)PlayerMovementState.Fall;
                }
                break;
        }
        return currentState;
    }
}
