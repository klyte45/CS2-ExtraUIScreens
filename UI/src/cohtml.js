!function(e){"object"==typeof module&&module.exports?module.exports=e(global,global.engine,!1):engine=e(this,window.engine,!0)}((
    function (e, t, n) {
        "use strict"; var r = function () {
            this.events = {}
        }
            , i = function (e, t) { this.code = e, this.context = t }
            , o = void 0 !== t; if ((t = t || {}
            )._Initialized) return t; var a = [2, 0, 2, 0]; for (var l in r.prototype._createClear = function (e, n, r, i) {
                return function () {
                    var o = e.events[n]; if (o) {
                        var a = -1; if (void 0 === r) {
                            for (var l = 0; l < o.length; ++l)if (void 0 !== o[l].wasInCPP) { a = l; break }
                        }
                        else a = o.indexOf(r); -1 != a && (o.splice(a, 1), 0 === o.length && delete e.events[n])
                    }
                    else void 0 !== t.RemoveOnHandler && t.RemoveOnHandler(n, r, i || t)
                }
            }
                , r.prototype.on = function (e, t, n) {
                    var r = this.events[e]; void 0 === r && (r = this.events[e] = []); var o = new i(t, n || this); return r.push(o), { clear: this._createClear(this, e, o, n) }
                }
                , r.prototype.off = function (e, n, r) {
                    var i = this.events[e]; if (void 0 !== i) {
                        var o; r = r || this; var a = i.length; for (o = 0; o < a; ++o) { var l = i[o]; if (l.code == n && l.context == r) break }
                        o < a && (i.splice(o, 1), 0 === i.length && delete this.events[e])
                    }
                    else t.RemoveOnHandler(e, n, r || this)
                }
                , r.prototype.trigger = function (e) {
                    var t = this.events[e]; if (void 0 !== t) {
                        var n = Array.prototype.slice.call(arguments, 1); return t.forEach((function (e) { e.code.apply(e.context, n) }
                        )), !0
                    }
                    return !1
                }
                , t.isAttached = o, t.isAttached || (r.prototype.on = function (e, t, n) {
                    var r = this.events[e]; this.browserCallbackOn && this.browserCallbackOn(e, t, n), void 0 === r && (r = this.events[e] = []); var o = new i(t, n || this); return r.push(o), { clear: this._createClear(this, e, o) }
                }
                    , r.prototype.off = function (e, t, n) {
                        var r = this.events[e]; if (void 0 !== r) {
                            var i; n = n || this; var o = r.length; for (i = 0; i < o; ++i) { var a = r[i]; if (a.code == t && a.context == n) break }
                            i < o && (r.splice(i, 1), 0 === r.length && (delete this.events[e], this.browserCallbackOff && this.browserCallbackOff(e, t, n)))
                        }
                    }
                    , t.SendMessage = function (e, n) {
                        var r = Array.prototype.slice.call(arguments, 2), i = t._ActiveRequests[n]; delete t._ActiveRequests[n]; var o = function () { var n = t._mocks[e]; void 0 !== n && i.resolve(n.apply(t, r)) }
                            ; window.setTimeout(o, 16)
                    }
                    , t.TriggerEvent = function () {
                        var e = Array.prototype.slice.call(arguments), n = function () { var n = t._mocks[e[0]]; void 0 !== n && n.apply(t, e.slice(1)) }
                        ; return window.setTimeout(n, 16), void 0 !== t._mocks[e[0]]
                    }
                    , t.BindingsReady = function () { t._OnReady() }
                    , t.createJSModel = function (t, n) { e[t] = n }
                    , t.updateWholeModel = function () { }
                    , t.synchronizeModels = function () { }
                    , t.enableImmediateLayout = function () { }
                    , t.isImmediateLayoutEnabled = function () { return !0 }
                    , t.executeImmediateLayoutSync = function () { }
                    , t._mocks = {}
                    , t._mockImpl = function (e, t, n, r) { t && (this._mocks[e] = t); var i = t.toString().replace("function " + t.name + "(", ""), o = i.indexOf(")"), a = i.substr(0, o); this.browserCallbackMock && this.browserCallbackMock(e, a, n, Boolean(r)) }
                    , t.mock = function (e, t, n) { this._mockImpl(e, t, !0, n) }
                ), t.events = {}
                , r.prototype) t[l] = r.prototype[l]; return t.isAttached && (t.on = function (e, n, r) {
                    return n ? (t.AddOrRemoveOnHandler(e, n, r || t), { clear: this._createClear(this, e, n, r) }
                    ) : (console.error("No handler specified for engine.on"), {
                        clear: function () { }
                    }
                    )
                }
                ), t.whenReady = new Promise((function (e) { t.on("Ready", e) }
                )), t._trigger = r.prototype.trigger, t.trigger = function () { this._trigger.apply(this, arguments) || this.TriggerEvent.apply(this, arguments) }
                    , t.isAttached && (t.mock = function () { }
                    ), t._BindingsReady = !1, t._WindowLoaded = !1, t._RequestId = 0, t._ActiveRequests = {}
                    , t.call = function () {
                        t._RequestId++; var e = t._RequestId, n = Array.prototype.slice.call(arguments); n.splice(1, 0, e); var r = new Promise((function (r, i) {
                            t._ActiveRequests[e] = { resolve: r, reject: i }
                            , t.SendMessage.apply(t, n)
                        }
                        )); return r
                    }
                    , t._Result = function (e) {
                        var n = t._ActiveRequests[e]; if (void 0 !== n) { delete t._ActiveRequests[e]; var r = Array.prototype.slice.call(arguments); r.shift(), n.resolve.apply(n, r) }
                    }
                    , t._Reject = function (e) {
                        var n = t._ActiveRequests[e]; void 0 !== n && (delete t._ActiveRequests[e], requestAnimationFrame((function () { return n.reject("No handler registered") }
                        )))
                    }
                    , t._ForEachError = function (e, t) { for (var n = e.length, r = 0; r < n; ++r)t(e[r].first, e[r].second) }
                    , t._TriggerError = function (e) { t.trigger("Error", e) }
                    , t._OnError = function (e, n) {
                        if (null === e || 0 === e) t._ForEachError(n, t._TriggerError); else { var r = t._ActiveRequests[e]; delete t._ActiveRequests[e], r.reject(new Error(n[0].second)) }
                        if (n.length) throw new Error(n[0].second)
                    }
                    , t._OnReady = function () { t._BindingsReady = !0, t._WindowLoaded && t.trigger("Ready") }
                    , t._OnWindowLoaded = function () { t._WindowLoaded = !0, t._BindingsReady && t.trigger("Ready") }
                    , n ? e.addEventListener("load", (function () { t._OnWindowLoaded() }
                    )) : t._WindowLoaded = !0, t.on("_Result", t._Result, t), t.on("_Reject", t._Reject, t), t.on("_OnReady", t._OnReady, t), t.on("_OnError", t._OnError, t), t.dependency = new WeakMap, t.hasAttachedUpdateListner = !1, t.onUpdateWholeModel = function (e) {
                        (t.dependency.get(e) || []).forEach((function (e) { return t.updateWholeModel(e) }
                        ))
                    }
                    , t.createObservableModel = function (e) {
                        var n = {
                            set: function (n, r, i) { t.updateWholeModel(window[e]), n[r] = i }
                        }
                        ; t.createJSModel(e, new Proxy({}
                            , n))
                    }
                    , t.addSynchronizationDependency = function (e, n) { t.hasAttachedUpdateListner || (t.addDataBindEventListner("updateWholeModel", t.onUpdateWholeModel), t.hasAttachedUpdateListner = !0); var r = t.dependency.get(e); r || (r = [], t.dependency.set(e, r)), r.push(n) }
                    , t.removeSynchronizationDependency = function (e, n) { var r = t.dependency.get(e) || []; r.splice(r.indexOf(n), 1) }
                    , t.BindingsReady(a[0], a[1], a[2], a[3]), t._Initialized = !0, t
    }
    ));