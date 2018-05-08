using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Compilation;
using System.Xml;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Localization;
using NBrightCore.common;
using NBrightDNN;
using Nevoweb.DNN.NBrightBuy.Components;
using Nevoweb.DNN.NBrightBuy.Components.Products;
using Nevoweb.DNN.NBrightBuy.Components.Interfaces;
using RazorEngine.Compilation.ImpromptuInterface.InvokeExt;
using DotNetNuke.Entities.Users;

namespace OpenStore.Providers.OS_BulkEdit
{
    public class AjaxProvider : AjaxInterface
    {
        public override string Ajaxkey { get; set; }

        public override string ProcessCommand(string paramCmd, HttpContext context, string editlang = "")
        {
            if (!NBrightBuyUtils.CheckRights())
            {
                return "Security Error.";
            }

            var ajaxInfo = NBrightBuyUtils.GetAjaxFields(context);
            var lang = NBrightBuyUtils.SetContextLangauge(ajaxInfo); // Ajax breaks context with DNN, so reset the context language to match the client.
            var objCtrl = new NBrightBuyController();

            var strOut = "OS_BulkEdit Ajax Error";

            // NOTE: The paramCmd MUST start with the plugin ref. in lowercase. (links ajax provider to cmd)
            switch (paramCmd)
            {
                case "os_bulkedit_test":
                    strOut = "<root>" + UserController.Instance.GetCurrentUserInfo().Username + "</root>";
                    break;
                case "os_bulkedit_getdata":
                    strOut = GetData(context);
                    break;
                case "os_bulkedit_addnew":
                    strOut = GetData(context, true);
                    break;
                case "os_bulkedit_deleterecord":
                    strOut = DeleteData(context);
                    break;
                case "os_bulkedit_savedata":
                    strOut = SaveData(context);
                    break;
                case "os_bulkedit_selectlang":
                    strOut = SaveData(context);
                    break;
            }

            return strOut;

        }

        public override void Validate()
        {
        }

        #region "Methods"

        private String GetData(HttpContext context, bool clearCache = false)
        {

            var objCtrl = new NBrightBuyController();
            var strOut = "";
            //get uploaded params
            var ajaxInfo = NBrightBuyUtils.GetAjaxFields(context);

            var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/itemid");
            var typeCode = ajaxInfo.GetXmlProperty("genxml/hidden/typecode");
            var newitem = ajaxInfo.GetXmlProperty("genxml/hidden/newitem");
            var selecteditemid = ajaxInfo.GetXmlProperty("genxml/hidden/selecteditemid");
            var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
            var editlang = ajaxInfo.GetXmlProperty("genxml/hidden/editlang");
            if (editlang == "") editlang = Utils.GetCurrentCulture();

            if (!Utils.IsNumeric(moduleid)) moduleid = "-2"; // use moduleid -2 for razor

            if (clearCache) NBrightBuyUtils.RemoveModCache(Convert.ToInt32(moduleid));

            if (newitem == "new") selecteditemid = AddNew(moduleid, typeCode);

            var templateControl = "/DesktopModules/NBright/OS_BulkEdit";

            if (Utils.IsNumeric(selecteditemid))
            {
                // do edit field data if a itemid has been selected
                var obj = objCtrl.Get(Convert.ToInt32(selecteditemid), "", editlang);
                strOut = NBrightBuyUtils.RazorTemplRender("datafields.cshtml", Convert.ToInt32(moduleid), itemid + editlang + selecteditemid, obj, templateControl, "config", editlang, StoreSettings.Current.Settings());
            }
            else
            {
                // Return list of items
                var l = objCtrl.GetList(PortalSettings.Current.PortalId, Convert.ToInt32(moduleid), typeCode, "", " order by [XMLData].value('(genxml/textbox/ref)[1]','nvarchar(50)')", 0, 0, 0, 0, editlang);
                strOut = NBrightBuyUtils.RazorTemplRenderList("datalist.cshtml", Convert.ToInt32(moduleid), editlang, l, templateControl, "config", editlang, StoreSettings.Current.Settings());
            }

            return strOut;

        }

        private String AddNew(String moduleid, String typeCode)
        {
            if (!Utils.IsNumeric(moduleid)) moduleid = "-2"; // -2 for razor

            var objCtrl = new NBrightBuyController();
            var nbi = new NBrightInfo(true);
            nbi.PortalId = PortalSettings.Current.PortalId;
            nbi.TypeCode = typeCode;
            nbi.ModuleId = Convert.ToInt32(moduleid);
            nbi.ItemID = -1;
            var itemId = objCtrl.Update(nbi);
            nbi.ItemID = itemId;

            foreach (var lang in DnnUtils.GetCultureCodeList(PortalSettings.Current.PortalId))
            {
                var nbi2 = new NBrightInfo(true);
                nbi2.PortalId = PortalSettings.Current.PortalId;
                nbi2.TypeCode = typeCode + "LANG";
                nbi2.ModuleId = Convert.ToInt32(moduleid);
                nbi2.ItemID = -1;
                nbi2.Lang = lang;
                nbi2.ParentItemId = itemId;
                nbi2.GUIDKey = "";
                nbi2.ItemID = objCtrl.Update(nbi2);
            }

            NBrightBuyUtils.RemoveModCache(nbi.ModuleId);

            return nbi.ItemID.ToString("");
        }

        private String SaveData(HttpContext context)
        {
            var objCtrl = new NBrightBuyController();

            //get uploaded params
            var ajaxInfo = NBrightBuyUtils.GetAjaxFields(context);

            var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/itemid");
            var editlang = ajaxInfo.GetXmlProperty("genxml/hidden/editlang");
            if (editlang == "") editlang = Utils.GetCurrentCulture();

            if (Utils.IsNumeric(itemid))
            {
                // get DB record
                var nbi = objCtrl.Get(Convert.ToInt32(itemid));
                if (nbi != null)
                {
                    var typecode = nbi.TypeCode;

                    // get data passed back by ajax
                    var strIn = HttpUtility.UrlDecode(Utils.RequestParam(context, "inputxml"));
                    // update record with ajax data
                    nbi.UpdateAjax(strIn);
                    nbi.GUIDKey = nbi.GetXmlProperty("genxml/textbox/ref");
                    objCtrl.Update(nbi);

                    // do langauge record
                    var nbi2 = objCtrl.GetDataLang(Convert.ToInt32(itemid), editlang);
                    nbi2.UpdateAjax(strIn);
                    objCtrl.Update(nbi2);

                    DataCache.ClearCache(); // clear ALL cache.

                }
            }
            return "";
        }

        private String DeleteData(HttpContext context)
        {
            var objCtrl = new NBrightBuyController();

            //get uploaded params
            var ajaxInfo = NBrightBuyUtils.GetAjaxFields(context);
            var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/itemid");
            if (Utils.IsNumeric(itemid))
            {
                // delete DB record
                objCtrl.Delete(Convert.ToInt32(itemid));

                NBrightBuyUtils.RemoveModCache(-2);
            }
            return "";
        }

        #endregion


        private Boolean CheckRights()
        {
            if (UserController.Instance.GetCurrentUserInfo().IsInRole(StoreSettings.ManagerRole) || UserController.Instance.GetCurrentUserInfo().IsInRole(StoreSettings.EditorRole) || UserController.Instance.GetCurrentUserInfo().IsInRole("Administrators"))
            {
                return true;
            }
            return false;
        }


    }
}
