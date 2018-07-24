-- Generated by CSharp.lua Compiler
local System = System
local UnityEngine = UnityEngine
System.namespace("CSLua", function (namespace)
  namespace.class("TestCoroutine", function (namespace)
    local Awake, OnTick, Test
    Awake = function (this)
      UnityEngine.Debug.Log0("TestCoroutine")
      MonoManager.getInstance():StartCoroutine0(OnTick(this))
    end
    OnTick = function (this)
      return System.yieldIEnumerator(function (this)
        while true do
          System.yieldReturn(UnityEngine.WaitForSeconds(1))
          UnityEngine.Debug.Log0("TestCoroutine.OnTick")
        end
      end, System.Object, this)
    end
    Test = function (this)
      UnityEngine.Debug.Log0("TestCoroutine.Test")
    end
    return {
      Awake = Awake,
      Test = Test
    }
  end)
end)
