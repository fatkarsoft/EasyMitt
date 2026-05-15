export default function PageTitle({ title, parent = "EasyMitt", action }) {
  return (
    <div className="row align-items-center page-title-row mb-4">
      <div className="col-sm-6 col-12">
        <div className="page-title-box">
          <h4 className="font-size-18 mb-1">{title}</h4>
          <ol className="breadcrumb mb-0">
            <li className="breadcrumb-item">{parent}</li>
            <li className="breadcrumb-item active">{title}</li>
          </ol>
        </div>
      </div>
      <div className="col-sm-6 col-12 page-title-action mt-3 mt-sm-0">{action}</div>
    </div>
  );
}
