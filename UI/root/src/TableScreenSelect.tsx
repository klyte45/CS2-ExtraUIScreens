///<reference path="cohtml.d.ts" />
///<reference path="euis.d.ts" />
import { CSSProperties, Component } from "react";
import { CheckboxTitleless } from "./components/checkbox";
import { ObjectTyped } from "object-typed";

const columnStyle: CSSProperties = {
  marginRight: 0,
  flex: "0 0 400px",
}
const modIconStyle: CSSProperties = {
  width: 40,
  height: 40,
  marginRight: 5
}
const modDisplayNameStyle: CSSProperties = {
  height: 20,
}
const modAppNameStyle: CSSProperties = {
  height: 20,
}
const columnStyleRight: CSSProperties = {
  marginRight: 0,
  textAlign: "center",
  flex: "0 0 100px",
}
const columnStyleRightCheck: CSSProperties = {
  marginRight: 0,
  textAlign: "center",
  flex: "0 0 100px",
}
const flexSpace: CSSProperties = {
  flex: 1,
}
export class TableScreenSelect extends Component<{ rootProps: RootProps }, {
  quantityMonitors: number;
  appsByMonitor: Record<number, string[]>;
  allApps: string[];
  activeMonitorMask: number;
}> {

  constructor(props: { rootProps: RootProps }) {
    super(props);
    this.state = {
      quantityMonitors: 1,
      appsByMonitor: {},
      allApps: [],
      activeMonitorMask: 0
    };
    const thisComponent = this;
    const loadData = () => TableScreenSelect.loadData(thisComponent, this.state.activeMonitorMask);
    engine.on("k45::euis.removeAppButton->", loadData)
    engine.on("k45::euis.reintroduceAppButton->", loadData)
    engine.on("k45::euis.activeMonitorMaskChange->", (newMonitorMask: number) => {
      TableScreenSelect.loadData(thisComponent, newMonitorMask);
    })
    engine.whenReady.then(async () => {
      const monitorMask: number = await engine.call("k45::euis.getActiveMonitorMask");
      TableScreenSelect.loadData(thisComponent, monitorMask)
    })
  }

  static async loadData(component: TableScreenSelect, activeMask: number) {
    const quantityMonitors = await component.props.rootProps.getQuantityMonitors();
    const appsActive = ObjectTyped.fromEntries(await Promise.all([...new Array(quantityMonitors)].map(async (_, i) => { return [(i + 1).toString(), await component.props.rootProps.listActiveAppsInMonitor(i + 1)] as [string, string[]]; })));
    component.setState({
      quantityMonitors: quantityMonitors,
      allApps: (await component.props.rootProps.getAppNames()).filter(x => x != "@k45-euis/root"),
      appsByMonitor: appsActive,
      activeMonitorMask: activeMask
    })
    component.forceUpdate()
  }

  private checkMonitorActive(monitorId: number) {
    return (this.state.activeMonitorMask & (1 << (monitorId - 1))) == 0
  }

  shouldComponentUpdate() { return true; }

  render() {
    const allAppData = this.props.rootProps.getAppData();
    const rootProps = this.props.rootProps;
    const thisComponent = this;
    const appRows = this.state.allApps.map((x, i) => {
      const appData = allAppData[x];
      return <div className="field__MBOM9 field__UuCZq" key={i}>
        <div style={modIconStyle} ><img src={appData.iconUrl} style={modIconStyle} /></div>
        <div className="label__DGc7_" style={columnStyle}>
          <div style={modDisplayNameStyle}>{appData.displayName}</div>
          <div style={modAppNameStyle}>{appData.appName}</div>
        </div>
        <div style={flexSpace}></div>
        {new Array(this.state.quantityMonitors).fill(null).map((_, i) => {
          if (this.checkMonitorActive(i + 1))
            return <div key={i} className="label__DGc7_" style={columnStyleRightCheck}>
              <CheckboxTitleless isChecked={() => { return thisComponent.state.appsByMonitor[i + 1]?.includes(x); }} onClick={(k) => {
                (k ? rootProps.reintroduceAppButton(x, i + 1) : rootProps.removeAppButton(x, i + 1));
              }}>
              </CheckboxTitleless>
            </div>;
        })}
      </div>
    })
    return <>
      <div className="field__MBOM9 field__UuCZq">
        <div className="label__DGc7_ label__ZLbNH" style={columnStyle}>{engine.translate("K45::EUIS.root[Table.AppNameColumn]")}</div>
        <div style={flexSpace}></div>
        {Array(this.state.quantityMonitors).fill(null).map((_, i) => {
          if (this.checkMonitorActive(i + 1))
            return <div key={i} className='label__DGc7_ label__ZLbNH' style={columnStyleRight}>{engine.translate("K45::EUIS.root[Table.ShowInMonitorCol]").replace("{0}", "" + (i + 1))}</div>;
        })}
      </div>
      {appRows}
      <div className="bottom-padding__JS3wW"></div>
    </>;
  }
}
