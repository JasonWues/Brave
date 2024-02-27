using Godot;

public partial class Player : CharacterBody2D
{
    public const float Speed = 160.0f;
    public const float JumpVelocity = -400.0f;
    public const float FloorAcceleration = (float)(Speed / 0.2);
    public const float AirAcceleration = (float)(Speed / 0.02);
    
    public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

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

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;
		
        var direction = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");

        var acceleration = IsOnFloor() ? FloorAcceleration : AirAcceleration;
        
        velocity.X = (float)Mathf.MoveToward(velocity.X,direction * Speed,acceleration * delta) ;
		
        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y += Gravity * (float)delta;

        var canJump = IsOnFloor() || _coyoteTimer.TimeLeft > 0;
        var shouldJump = canJump && _jumpRequestTimer.TimeLeft > 0;
        // Handle Jump.
        if (shouldJump)
        {
            velocity.Y = JumpVelocity;
            _coyoteTimer.Stop();
            _jumpRequestTimer.Stop();
        }


        if (IsOnFloor())
        {
            if (Mathf.IsZeroApprox(direction) && Mathf.IsZeroApprox(velocity.X))
            {
                _animationPlayer.Play("idle");
            }
            else
            {
                _animationPlayer.Play("running");
            }

        }
        else
        {
            _animationPlayer.Play("jump");
        }

        if (!Mathf.IsZeroApprox(direction))
        {
            _sprite2D.FlipH = direction < 0;
        }

        var wasOnFloor = IsOnFloor();
		
        Velocity = velocity;
        MoveAndSlide();

        if (IsOnFloor() != wasOnFloor)
        {
            if (wasOnFloor && !shouldJump)
            {
                _coyoteTimer.Start();
            }
            else
            {
                _coyoteTimer.Stop();
            }
        }
    }
}
