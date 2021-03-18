﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class GameObject
{
    public Vector2 position;
    public Vector2 size;
    public Vector2 velocity;
    public Texture2D texture;

    public GameObject(String assetName)
    {
        texture = GameEnvironment.ContentManager.Load<Texture2D>(assetName);
        Init();
    }

    public virtual void Update() { }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(texture, new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y), Color.White);
    }

    public virtual void Init()
    {

    }

    public Boolean Overlaps(GameObject other)
    {
        float w0 = this.size.X,
            h0 = this.size.Y,
            w1 = other.size.X,
            h1 = other.size.Y,
            x0 = this.position.X,
            y0 = this.position.Y,
            x1 = other.position.X,
            y1 = other.position.Y;

        return !(x0 > x1 + w1 || x0 + w0 < x1 ||
          y0 > y1 + h1 || y0 + h0 < y1);
    }
}
