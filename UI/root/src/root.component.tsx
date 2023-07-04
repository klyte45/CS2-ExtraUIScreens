///<reference path="euis.d.ts" />

import { CSSProperties } from "react";
import { TableScreenSelect } from "./TableScreenSelect";

export default function Root(props: RootProps) {
  let tableStyle: CSSProperties = {
    position: "absolute",
    top: 160,
    left: 20,
    right: 20,
    bottom: 70,
    borderWidth: 1,
    "borderStyle": "solid",
    "borderColor": "rgba(0, 0, 0, 0.4)",
    backgroundColor: "rgba(0,0,0,0.6)"
  }
  let h1: CSSProperties = {
    textAlign: "center",
    color: "white"
  }

  const bottomBar: CSSProperties = {
    position: "fixed",
    bottom: 20,
    left: 20,
    right: 20,
    height: 35,
    display: 'flex',
    flexDirection: "row-reverse",
  }
  const btnBottomBar: CSSProperties = {
    display: 'flex',
    height: 35
  }

  var el = <>
    <h1 style={h1}>{engine.translate("K45::EUIS.root[MonitorTitle]").replace("{0}", "" + __euis_main.monitorNumber)}</h1>
    <h2 style={h1}>{engine.translate("K45::EUIS.root[MonitorSubtitle]")}</h2>
    <div className="main-column__D0AW2" style={tableStyle}>
      <div className="scrollable__DXr5q y__SMM6s scrollable__Ptfvi">
        <section id="tableContent">
          <TableScreenSelect rootProps={props}></TableScreenSelect>
        </section>
        <div className="track__e3Ogi y__SMM6s">
          <div className="thumb__CiblF y__SMM6s">
          </div>
        </div>
      </div>
    </div>
    <div style={bottomBar}>
      <button onClick={() => engine.call("k45::euis.saveAppSelectionAsDefault")} className="button__WWaYD button__SH8X2" style={btnBottomBar}>{engine.translate("K45::EUIS.root[SaveAppsAsDefault]")}</button>
    </div>
  </>;
  return el;
}
