///<reference path="euis.d.ts" />

import { CSSProperties, Component } from "react";
import { TableScreenSelect } from "./TableScreenSelect";
import { AppProps } from "single-spa";

let tableStyle: CSSProperties = {
  position: "absolute",
  top: "160rem",
  left: "20rem",
  right: "20rem",
  bottom: "70rem",
  borderWidth: 1,
  "borderStyle": "solid",
  "borderColor": "rgba(0, 0, 0, 0.4)",
  backgroundColor: "rgba(0,0,0,0.6)",
  color: "#EEEEEE"
}
let h1: CSSProperties = {
  textAlign: "center",
  color: "white"
}

const bottomBar: CSSProperties = {
  position: "fixed",
  bottom: "20rem",
  left: "20rem",
  right: "20rem",
  height: "35rem",
  display: 'flex',
  flexDirection: "row-reverse",
}
const btnBottomBar: CSSProperties = {
  display: 'flex',
  height: "35rem"
}

export default class Root extends Component<RootProps & AppProps, {}>{

  componentDidMount() {
    engine.whenReady.then(() => {
      engine.on("k45::euis.localeChanged", () => this.setState({}))
    })
  }
  
  render() {
    return <>
      <h1 style={h1}>{engine.translate("K45::EUIS.root[MonitorTitle]").replace("{0}", "" + __euis_main.monitorNumber)}</h1>
      <h2 style={h1}>{engine.translate("K45::EUIS.root[MonitorSubtitle]")}</h2>
      <div className="main-column__D0AW2" style={tableStyle}>
        <div className="scrollable__DXr5q y__SMM6s scrollable__Ptfvi">
          <section id="tableContent">
            <TableScreenSelect rootProps={this.props}></TableScreenSelect>
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
  }
}