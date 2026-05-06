export default function PageTitle({ title, parent = "EasyMitt", action }) {
  return (
    <div className="row align-items-center mb-4">
      <div className="col-sm-6">
        <div className="page-title-box">
          <h4 className="font-size-18 mb-1">{title}</h4>
          <ol className="breadcrumb mb-0">
            <li className="breadcrumb-item">{parent}</li>
            <li className="breadcrumb-item active">{title}</li>
          </ol>
        </div>
      </div>
      <div className="col-sm-6 text-sm-right mt-3 mt-sm-0">{action}</div>
    </div>
  );
}
