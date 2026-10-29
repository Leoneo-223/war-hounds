using Godot;

public partial class PlayerController : CharacterBody3D
{
    [ExportGroup("Speed")]
    [Export] public float CrouchSpeed = 1.8f;
    [Export] public float WalkSpeed = 3f;
    [Export] public float RunSpeed = 5.5f;
    [Export] public float SprintSpeed = 8f;
    [Export] public float JumpVelocity = 7f;

    [ExportGroup("Camera")]
    [Export] public float MouseSensitivity = 0.1f;   // degrees per pixel

    [ExportGroup("Height")]
    [Export] public float StandHeight = 1.85f;
    [Export] public float CrouchHeight = 1.38f;
    [Export] public float HeadOffset = 0.25f;        // how far below the top the camera sits

    [ExportGroup("Head Bob")]
    [Export] public bool BobEnabled = true;
    [Export] public float BobFrequency = 2.2f;       // steps per meter travelled
    [Export] public float BobAmplitudeY = 0.06f;     // vertical movement (meters)
    [Export] public float BobAmplitudeX = 0.03f;     // side-to-side movement (meters)
    [Export] public float BobSmoothing = 12f;        // how fast the bob fades in/out

    [ExportGroup("Speed FOV")]
    [Export] public bool FovEffectEnabled = true;
    [Export] public float WalkFovBoost = 1f;         // degrees added when walking
    [Export] public float RunFovBoost = 3f;          // degrees added when running
    [Export] public float SprintFovBoost = 9f;       // degrees added when sprinting
    [Export] public float FovSmoothing = 6f;         // how fast the FOV changes

    private float _speed;
    private float _landingVelocity;
    private bool _jumpWasDown;

    private float _bobTime;
    private Vector3 _bobOffset;
    private float _landingDip;
    private float _baseFov;

    private CollisionShape3D _collision;
    private CapsuleShape3D _shape;
    private MeshInstance3D _mesh;
    private Node3D _pivot;
    private Camera3D _camera;

    public override void _Ready()
    {
        _collision = GetNode<CollisionShape3D>("CollisionShape3D");
        _mesh = GetNode<MeshInstance3D>("MeshInstance3D");
        _pivot = GetNode<Node3D>("Pivot");
        _camera = GetNode<Camera3D>("Pivot/Camera3D");

        _baseFov = _camera.Fov;   // uses whatever FOV the camera has in the scene

        _shape = _collision.Shape as CapsuleShape3D;
        if (_shape == null)
        {
            GD.PushError("Player CollisionShape3D must use a CapsuleShape3D.");
            SetPhysicsProcess(false);
            return;
        }

        _speed = RunSpeed;
        _mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouseMotion motion)
        {
            // Turn the body left/right
            Vector3 body = RotationDegrees;
            body.Y -= motion.Relative.X * MouseSensitivity;
            RotationDegrees = body;

            // Tilt the pivot up/down
            Vector3 pivot = _pivot.RotationDegrees;
            pivot.X = Mathf.Clamp(pivot.X - motion.Relative.Y * MouseSensitivity, -90f, 90f);
            _pivot.RotationDegrees = pivot;
        }
    }

    private static bool KeyDown(Key key) => Input.IsPhysicalKeyPressed(key);

    public override void _PhysicsProcess(double delta)
    {
        float d = (float)delta;
        var shape = _shape;

        Vector3 velocity = Velocity;

        // Gravity
        if (!IsOnFloor())
        {
            velocity += GetGravity() * 2f * d;
            _landingVelocity = -velocity.Y;
        }

        // Jump (Space), only on the frame it is pressed
        bool jumpDown = KeyDown(Key.Space);
        if (jumpDown && !_jumpWasDown && IsOnFloor())
            velocity.Y = JumpVelocity;
        _jumpWasDown = jumpDown;

        bool crouching = KeyDown(Key.Ctrl) || KeyDown(Key.C);
        bool walking = KeyDown(Key.Alt);
        bool sprinting = KeyDown(Key.Shift) && KeyDown(Key.W);   // only sprint while moving forward

        // Only stand up if there's room. This doesn't force a crouch while standing.
        bool wantCrouch = crouching;
        if (!wantCrouch && shape.Height < StandHeight - 0.01f)
        {
            float needed = StandHeight - shape.Height + 0.05f;
            if (TestMove(GlobalTransform, Vector3.Up * needed))
                wantCrouch = true;
        }

        shape.Height = Mathf.Lerp(shape.Height, wantCrouch ? CrouchHeight : StandHeight, 0.1f);

        // Keep the capsule's bottom at the player's feet so it only grows upward
        _collision.Position = new Vector3(_collision.Position.X, shape.Height / 2f, _collision.Position.Z);
        _mesh.Position = new Vector3(_mesh.Position.X, shape.Height / 2f, _mesh.Position.Z);

        if (IsOnFloor())
        {
            if (_landingVelocity != 0)
            {
                StartLandingDip(_landingVelocity);
                _landingVelocity = 0;
            }

            _speed = RunSpeed;
            if (crouching) _speed = CrouchSpeed;
            else if (walking) _speed = WalkSpeed;
            else if (sprinting) _speed = SprintSpeed;
        }

        // Keep the mesh and head matched to the shape
        if (_mesh.Mesh is CapsuleMesh capsule)
            capsule.Height = shape.Height;
        _pivot.Position = new Vector3(_pivot.Position.X, shape.Height - HeadOffset, _pivot.Position.Z);

        // Movement
        Vector2 input = Vector2.Zero;
        if (KeyDown(Key.D)) input.X += 1;
        if (KeyDown(Key.A)) input.X -= 1;
        if (KeyDown(Key.S)) input.Y += 1;
        if (KeyDown(Key.W)) input.Y -= 1;
        input = input.LimitLength(1f);

        Vector3 direction = (Transform.Basis * new Vector3(input.X, 0, input.Y)).Normalized();

        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * _speed;
            velocity.Z = direction.Z * _speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0, _speed);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, _speed);
        }

        Velocity = velocity;
        MoveAndSlide();

        UpdateCamera(d);
    }

    private void UpdateCamera(float d)
    {
        // Horizontal speed actually achieved this frame (0 when pushing into a wall)
        Vector3 real = GetRealVelocity();
        float horizontalSpeed = new Vector2(real.X, real.Z).Length();

        // ---- Head bob ----
        Vector3 targetBob = Vector3.Zero;

        if (BobEnabled && IsOnFloor() && horizontalSpeed > 0.1f)
        {
            // Advance the phase by distance travelled, so the bob matches the footsteps
            _bobTime += horizontalSpeed * d * BobFrequency * Mathf.Pi;

            targetBob = new Vector3(
                Mathf.Cos(_bobTime * 0.5f) * BobAmplitudeX,
                Mathf.Sin(_bobTime) * BobAmplitudeY,
                0f);
        }

        // Smoothly blend in and out so it never snaps
        _bobOffset = _bobOffset.Lerp(targetBob, 1f - Mathf.Exp(-BobSmoothing * d));

        // Landing dip eases back to zero
        _landingDip = Mathf.Lerp(_landingDip, 0f, 1f - Mathf.Exp(-8f * d));

        // ---- Speed FOV ----
        // Stages: standing/crouching = base, walking = tiny boost, running = slight boost, sprinting = most
        float targetFov = _baseFov;
        if (FovEffectEnabled)
        {
            float boost;
            if (horizontalSpeed <= CrouchSpeed)
                boost = 0f;
            else if (horizontalSpeed <= WalkSpeed)
                boost = Mathf.Lerp(0f, WalkFovBoost, Mathf.InverseLerp(CrouchSpeed, WalkSpeed, horizontalSpeed));
            else if (horizontalSpeed <= RunSpeed)
                boost = Mathf.Lerp(WalkFovBoost, RunFovBoost, Mathf.InverseLerp(WalkSpeed, RunSpeed, horizontalSpeed));
            else
                boost = Mathf.Lerp(RunFovBoost, SprintFovBoost, Mathf.Clamp(Mathf.InverseLerp(RunSpeed, SprintSpeed, horizontalSpeed), 0f, 1f));

            targetFov = _baseFov + boost;
        }
        _camera.Fov = Mathf.Lerp(_camera.Fov, targetFov, 1f - Mathf.Exp(-FovSmoothing * d));

        _camera.Position = new Vector3(_bobOffset.X, _bobOffset.Y + _landingDip, 0f);
    }

    private void StartLandingDip(float landingVelocity)
    {
        float amplitude = Mathf.Clamp(landingVelocity / 100f, 0f, 0.3f);
        _landingDip = -amplitude;
    }
}