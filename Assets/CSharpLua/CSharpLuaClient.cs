﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using LuaInterface;

namespace CSharpLua {
  [LuaAutoWrap]
  public sealed class BridgeMonoBehaviour : MonoBehaviour {
    public LuaTable Table { get; private set; }
    public string LuaClass;
    public string SerializeData;

    public void Bind(LuaTable table) {
      Table = table;
      LuaClass = (string)table["__name__"];
    }

    internal void Bind(string luaClass, string serializeData) {
      LuaClass = luaClass;
      SerializeData = serializeData;
    }

    public Coroutine StartCoroutine(LuaTable routine) {
      return StartCoroutine(new LuaIEnumerator(routine));
    }

    public void RegisterUpdate(LuaFunction updateFn) {
      StartCoroutine(StartUpdate(updateFn));
    }

    private IEnumerator StartUpdate(LuaFunction updateFn) {
      while (true) {
        yield return null;
        updateFn.Call(Table);
      }
    }

    private void Awake() {
      if (!string.IsNullOrEmpty(LuaClass)) {
        Table = CSharpLuaClient.Instance.BindLua(this);
      }
    }

    private void Start() {
      using (var fn = Table.GetLuaFunction("Start")) {
        fn.Call(Table);
      }
    }
  }

  public sealed class LuaIEnumerator : IEnumerator, IDisposable {
    private LuaTable table_;
    private LuaFunction current_;
    private LuaFunction moveNext_;

    public LuaIEnumerator(LuaTable table) {
      table_ = table;
      current_ = table.GetLuaFunction("getCurrent");
      if (current_ == null) {
        throw new ArgumentNullException();
      }
      moveNext_ = table.GetLuaFunction("MoveNext");
      if (moveNext_ == null) {
        throw new ArgumentNullException();
      }
    }

    public object Current {
      get {
        return current_.Invoke<LuaTable, object>(table_);
      }
    }

    public void Dispose() {
      if (current_ != null) {
        current_.Dispose();
        current_ = null;
      }

      if (moveNext_ != null) {
        moveNext_.Dispose();
        moveNext_ = null;
      }

      if (table_ != null) {
        table_.Dispose();
        table_ = null;
      }
    }

    public bool MoveNext() {
      bool hasNext = moveNext_.Invoke<LuaTable, bool>(table_);
      if (!hasNext) {
        Dispose();
      }
      return hasNext;
    }

    public void Reset() {
      throw new NotSupportedException();
    }
  }

  public static class Consts {
    public const bool IsRunFromLua = true;
  }

  public class CSharpLuaClient : LuaClient {
    public string[] Components;
    private LuaFunction bindFn_;
    public static new CSharpLuaClient Instance { get { return (CSharpLuaClient)LuaClient.Instance; } }

    protected override void OpenLibs() {
      base.OpenLibs();
      OpenCJson();
    }

    public override void Destroy() {
      if (bindFn_ != null) {
        bindFn_.Dispose();
        bindFn_ = null;
      }
      base.Destroy();
    }

    protected override void StartMain() {
      if (Consts.IsRunFromLua) {
        base.StartMain();
        bindFn_ = luaState.GetFunction("UnityEngine.bind");
        if (bindFn_ == null) {
          throw new ArgumentNullException();
        }
        if (Components != null && Components.Length > 0) {
          using (var fn = luaState.GetFunction("UnityEngine.addComponent")) {
            foreach (string type in Components) {
              fn.Call(gameObject, type);
            }
          }
        }
      } else {
        if (Components != null) {
          foreach (string type in Components) {
            gameObject.AddComponent(Type.GetType(type, true, false));
          }
        }
      }
    }

    internal LuaTable BindLua(BridgeMonoBehaviour bridgeMonoBehaviour) {
      return bindFn_.Invoke<BridgeMonoBehaviour, string, string, LuaTable>(bridgeMonoBehaviour, bridgeMonoBehaviour.LuaClass, bridgeMonoBehaviour.SerializeData);
    }
  }
}

