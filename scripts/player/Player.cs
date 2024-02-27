using Godot;

public partial class Player : CharacterBody2D
{
    public const float Speed = 300.0f;
    public const float JumpVelocity = -400.0f;

    public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    private AnimationPlayer _animationPlayer;

    private Sprite2D _sprite2D;

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _sprite2D = GetNode<Sprite2D>("Sprite2D");
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;
		
        var direction = Input.GetAxis("move_left", "move_right");
        velocity.X = direction * Speed;
		
        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y += Gravity * (float)delta;

        // Handle Jump.
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            velocity.Y = JumpVelocity;

        if (IsOnFloor())
        {
            _animationPlayer.Play(Mathf.IsZeroApprox(direction) ? "idle" : "running");
        }
        else
        {
            _animationPlayer.Play("jump");
        }

        if (!Mathf.IsZeroApprox(direction))
        {
            _sprite2D.FlipH = direction < 0;
        }
		
        Velocity = velocity;
        MoveAndSlide();
    }
}
