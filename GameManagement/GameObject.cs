﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;

class GameObject
{
    public Vector2 position;
    public Vector2 velocity;
    public Texture2D texture;
    public MouseState mouse;

    public GameObject(String assetName)
    {
        texture = GameEnvironment.ContentManager.Load<Texture2D>(assetName);
        Init();
    }

    public virtual void Update()
    {
        //Mouse collsion
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(texture, position, Color.White);
    }

    public virtual void Init()
    {

    }

    public Boolean Overlaps(GameObject other)
    {
        float w0 = this.texture.Width,
            h0 = this.texture.Height,
            w1 = other.texture.Width,
            h1 = other.texture.Height,
            x0 = this.position.X,
            y0 = this.position.Y,
            x1 = other.position.X,
            y1 = other.position.Y;

        return !(x0 > x1 + w1 || x0 + w0 < x1 ||
          y0 > y1 + h1 || y0 + h0 < y1);
    }

    public Boolean MouseCollission()
    {
        float w0 = this.texture.Width,
          h0 = this.texture.Height,
          w1 = mouse.X,
          h1 = mouse.Y,
          x0 = this.position.X,
          y0 = this.position.Y,
          x1 = mouse.X,
          y1 = mouse.Y;

        return !(x0 > x1 + w1 || x0 + w0 < x1 ||
    y0 > y1 + h1 || y0 + h0 < y1);
    }

}

