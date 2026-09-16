using System.Numerics;
using PathTracerSceneGraph;

namespace PathTracerApp.Renderer;

public class TestMove : Component
{
    public float Speed { get; set; } = 1.0f;

    private Vector2 _direction;

    public override void Awake()
    {
        float angle = Random.Shared.NextSingle() * MathF.Tau;
        _direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    }

    public override void Update(float deltaTime)
    {
        var transform = Parent.GetComponent<Transform>();
        Vector3 position = transform.Position;

        position.X += _direction.X * Speed * deltaTime;
        position.Y += _direction.Y * Speed * deltaTime;

        if (position.X <= 0f)
        {
            position.X = 0f;
            _direction.X = MathF.Abs(_direction.X);
        }
        else if (position.X >= 1f)
        {
            position.X = 1f;
            _direction.X = -MathF.Abs(_direction.X);
        }

        if (position.Y <= 0f)
        {
            position.Y = 0f;
            _direction.Y = MathF.Abs(_direction.Y);
        }
        else if (position.Y >= 1f)
        {
            position.Y = 1f;
            _direction.Y = -MathF.Abs(_direction.Y);
        }

        transform.Position = position;
    }
}